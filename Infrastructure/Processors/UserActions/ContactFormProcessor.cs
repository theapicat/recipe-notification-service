using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Configurations;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors.UserActions;

public class ContactFormProcessor(
    ITemplateRenderService templateRenderService,
    IPendingEmailService pendingEmailService,
    IOptions<SmtpSettings> smtpSettings,
    ILogger<ContactFormProcessor> logger) : IContactFormProcessor
{
    private readonly SmtpSettings _settings = smtpSettings.Value;

    public async Task ProcessAsync(ContactFormSubmittedEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starter prosessering av kontaktskjema fra {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            email = eventData.Email,
            subject = eventData.Subject,
            message = eventData.Message,
            submitted_at = eventData.SubmittedAt.ToString("dd.MM.yyyy HH:mm")
        };

        // 1. Send e-post til Administrator / Support
        var adminHtml =
            await templateRenderService.RenderTemplateAsync("UserActions/ContactFormAdminNotification", templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            _settings.AdminNotificationEmail,
            $"[Kontaktskjema] {eventData.Subject}",
            adminHtml,
            nameof(ContactFormSubmittedEvent),
            cancellationToken
        );

        // 2. Send kvittering til brukeren
        var userReceiptHtml =
            await templateRenderService.RenderTemplateAsync("UserActions/ContactFormUserReceipt", templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            $"Takk for din henvendelse: {eventData.Subject}",
            userReceiptHtml,
            nameof(ContactFormSubmittedEvent),
            cancellationToken
        );
    }
}