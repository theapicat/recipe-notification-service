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
    IEmailDeliveryService emailDelivery,
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

        // 1. Send e-post til Administrator / Support (stien oppdatert med UserActions/)
        var adminHtml = await templateRenderService.RenderTemplateAsync("UserActions/ContactFormAdminNotification", templateModel);

        await emailDelivery.SendEmailAsync(
            to: _settings.AdminNotificationEmail,
            subject: $"[Kontaktskjema] {eventData.Subject}",
            htmlBody: adminHtml,
            replyTo: eventData.Email,
            cancellationToken: cancellationToken
        );

        // 2. Send kvittering til brukeren (stien oppdatert med UserActions/)
        var userReceiptHtml = await templateRenderService.RenderTemplateAsync("UserActions/ContactFormUserReceipt", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: $"Takk for din henvendelse: {eventData.Subject}",
            htmlBody: userReceiptHtml,
            cancellationToken: cancellationToken
        );
    }
}