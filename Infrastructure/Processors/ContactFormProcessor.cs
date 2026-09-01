using Contracts.Events;
using Infrastructure.EmailDelivery.Configurations;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors;

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

        // Klargjør anonymt modell-objekt med formatert dato for Scriban-malene
        var templateModel = new
        {
            Name = eventData.Name,
            Email = eventData.Email,
            Subject = eventData.Subject,
            Message = eventData.Message,
            SubmittedAt = eventData.SubmittedAt.ToString("dd.MM.yyyy HH:mm")
        };

        // 1. Generer og send e-post til Administrator / Support
        var adminHtml = await templateRenderService.RenderTemplateAsync("ContactFormAdminNotification", templateModel);

        await emailDelivery.SendEmailAsync(
            to: _settings.AdminNotificationEmail,
            subject: $"[Kontaktskjema] {eventData.Subject}",
            htmlBody: adminHtml,
            replyTo: eventData.Email,
            cancellationToken: cancellationToken
        );

        // 2. Generer og send kvittering til brukeren som sendte henvendelsen
        var userReceiptHtml = await templateRenderService.RenderTemplateAsync("ContactFormUserReceipt", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: $"Takk for din henvendelse: {eventData.Subject}",
            htmlBody: userReceiptHtml,
            cancellationToken: cancellationToken
        );

        logger.LogInformation("E-post til admin og kvittering til {Email} er sendt ut.", eventData.Email);
    }
}