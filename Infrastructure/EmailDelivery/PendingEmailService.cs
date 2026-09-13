using System.Net;
using Contracts.Events;
using Infrastructure.EmailDelivery.Configurations;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Exceptions;
using Infrastructure.State.Interfaces;
using Infrastructure.TemplateService.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence.Entities;
using Persistence.Repositories.Interfaces;

namespace Infrastructure.EmailDelivery;

public class PendingEmailService(
    IEmailDeliveryService emailDeliveryService,
    IFailedNotificationRepository failedNotificationRepository,
    INotificationStateStore stateStore,
    IPublishEndpoint publishEndpoint,
    ITemplateRenderService templateRenderService,
    IOptions<SmtpSettings> smtpSettings,
    ILogger<PendingEmailService> logger) : IPendingEmailService
{
    private const int MaxAttempts = 5;
    private readonly SmtpSettings _smtpSettings = smtpSettings.Value;

    public async Task<bool> ProcessEmailWithRetryAsync(
        string to,
        string subject,
        string htmlBody,
        string eventType,
        CancellationToken cancellationToken = default,
        Guid? existingNotificationId = null)
    {
        var lastErrorMessage = string.Empty;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            try
            {
                logger.LogInformation("Utsendingsforsøk {Attempt}/{MaxAttempts} for e-post til {To}", attempt,
                    MaxAttempts, to);

                await emailDeliveryService.SendEmailAsync(to, subject, htmlBody, cancellationToken: cancellationToken);

                if (existingNotificationId.HasValue)
                    await failedNotificationRepository.DeleteAsync(existingNotificationId.Value, cancellationToken);

                return true; // Vellykket utsending
            }
            catch (EmailDeliveryException ex)
            {
                lastErrorMessage = ex.Message;
                logger.LogWarning(ex, "Utsendingsforsøk {Attempt} feilet for {To}", attempt, to);

                // Sjekk om e-posten er permanent avvist (Hard bounce / ugyldig mottaker)
                if (IsHardBounce(ex))
                {
                    logger.LogError("Hard bounce registrert for {To}. Avbryter gjenforsøk og varsler Auth API.", to);

                    await publishEndpoint.Publish(new InvalidEmailDetectedEvent
                    {
                        Email = to,
                        Reason = ex.Message,
                        DetectedAt = DateTime.UtcNow
                    }, cancellationToken);

                    // Ikke levert - et evt. bufferdokument beholdes uendret til admin rydder opp manuelt
                    return false;
                }

                if (attempt < MaxAttempts) await Task.Delay(TimeSpan.FromSeconds(2 * attempt), cancellationToken);
            }

        // Alle 5 in-line forsøk feilet
        logger.LogError("Alle {MaxAttempts} in-line forsøk feilet for e-post til {To}. Lagrer i MongoDB.", MaxAttempts,
            to);

        if (existingNotificationId.HasValue)
        {
            // Oppdater det eksisterende bufferdokumentet i stedet for å opprette et duplikat
            await failedNotificationRepository.MarkRetryFailedAsync(existingNotificationId.Value, lastErrorMessage,
                MaxAttempts, cancellationToken);
        }
        else
        {
            var failedNotification = new FailedNotification
            {
                RecipientEmail = to,
                Subject = subject,
                HtmlBody = htmlBody,
                EventType = eventType,
                LastErrorMessage = lastErrorMessage,
                RetryCount = MaxAttempts,
                LastAttemptAt = DateTime.UtcNow
            };

            await failedNotificationRepository.AddAsync(failedNotification, cancellationToken);
        }

        await HandleAdminNotificationAsync(to, subject, lastErrorMessage, cancellationToken);

        return false;
    }

    private static bool IsHardBounce(EmailDeliveryException ex)
    {
        var message = ex.Message.ToLowerInvariant();
        if (ex.InnerException != null) message += " " + ex.InnerException.Message.ToLowerInvariant();

        return message.Contains("550") ||
               message.Contains("user unknown") ||
               message.Contains("recipient address rejected") ||
               message.Contains("mailbox unavailable") ||
               message.Contains("does not exist");
    }

    private async Task HandleAdminNotificationAsync(string recipient, string subject, string errorMessage,
        CancellationToken cancellationToken)
    {
        if (!stateStore.HasNotifiedAdmin)
        {
            stateStore.SetPendingStatus(true);
            stateStore.SetAdminNotified(true);

            logger.LogWarning("Minst én ubehandlet e-post ligger i bufferen. Sender avduplisert varsel til admin.");

            try
            {
                var templateModel = new
                {
                    recipient = WebUtility.HtmlEncode(recipient),
                    subject = WebUtility.HtmlEncode(subject),
                    error_message = WebUtility.HtmlEncode(errorMessage)
                };

                var adminBody =
                    await templateRenderService.RenderTemplateAsync("AdminActions/PendingEmailAlert", templateModel);

                await emailDeliveryService.SendEmailAsync(_smtpSettings.AdminNotificationEmail,
                    "[KRITISK] E-postutsendelse feilet i Kjøkkenhylla", adminBody,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Klarte ikke å sende varslings-e-post til administrator.");
            }
        }
        else
        {
            stateStore.SetPendingStatus(true);
        }
    }
}