using Contracts.Events;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Exceptions;
using Infrastructure.State.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using Persistence.Entities;
using Persistence.Repositories.Interfaces;

namespace Infrastructure.EmailDelivery;

public class PendingEmailService(
    IEmailDeliveryService emailDeliveryService,
    IFailedNotificationRepository failedNotificationRepository,
    INotificationStateStore stateStore,
    IPublishEndpoint publishEndpoint,
    ILogger<PendingEmailService> logger) : IPendingEmailService
{
    private const int MaxAttempts = 5;

    public async Task ProcessEmailWithRetryAsync(
        string to,
        string subject,
        string htmlBody,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        string lastErrorMessage = string.Empty;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                logger.LogInformation("Utsendingsforsøk {Attempt}/{MaxAttempts} for e-post til {To}", attempt, MaxAttempts, to);
                
                await emailDeliveryService.SendEmailAsync(to, subject, htmlBody, cancellationToken: cancellationToken);
                return; // Vellykket utsending
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

                    return;
                }

                if (attempt < MaxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt), cancellationToken);
                }
            }
        }

        // Alle 5 in-line forsøk feilet -> Lagre i MongoDB-buffer
        logger.LogError("Alle {MaxAttempts} in-line forsøk feilet for e-post til {To}. Lagrer i MongoDB.", MaxAttempts, to);

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

        await HandleAdminNotificationAsync(to, subject, lastErrorMessage, cancellationToken);
    }

    private static bool IsHardBounce(EmailDeliveryException ex)
    {
        var message = ex.Message.ToLowerInvariant();
        if (ex.InnerException != null)
        {
            message += " " + ex.InnerException.Message.ToLowerInvariant();
        }

        return message.Contains("550") ||
               message.Contains("user unknown") ||
               message.Contains("recipient address rejected") ||
               message.Contains("mailbox unavailable") ||
               message.Contains("does not exist");
    }

    private async Task HandleAdminNotificationAsync(string recipient, string subject, string errorMessage, CancellationToken cancellationToken)
    {
        if (!stateStore.HasNotifiedAdmin)
        {
            stateStore.SetPendingStatus(true);
            stateStore.SetAdminNotified(true);

            logger.LogWarning("Minst én ubehandlet e-post ligger i bufferen. Sender avduplisert varsel til admin.");

            try
            {
                const string adminEmail = "admin@kjokkenhylla.no";
                var adminBody = $"<h3>Systemvarsel: Feil ved e-postutsendelse</h3>" +
                                $"<p>Det har oppstått ubehandlede e-postfeil i systemet som krever ettersyn.</p>" +
                                $"<p><b>Mottaker:</b> {recipient}</p>" +
                                $"<p><b>Emne:</b> {subject}</p>" +
                                $"<p><b>Siste feil:</b> {errorMessage}</p>";

                await emailDeliveryService.SendEmailAsync(adminEmail, "[KRITISK] E-postutsendelse feilet i Kjøkkenhylla", adminBody, cancellationToken: cancellationToken);
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