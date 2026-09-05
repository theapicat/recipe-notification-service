using Contracts.Events.SystemActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.Interfaces.SystemActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors.SystemActions;

public class Confirmation14DaysReminderProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    IOptions<AppSettings> appSettings,
    ILogger<Confirmation14DaysReminderProcessor> logger) : IConfirmation14DaysReminderProcessor
{
    public async Task ProcessAsync(Confirmation14DaysReminderEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender varsel om sperret konto (14 dager) til {Email}", eventData.Email);

        var termsLink = $"{appSettings.Value.FrontendUrl.TrimEnd('/')}/legal/terms";

        var templateModel = new
        {
            name = eventData.Name,
            confirmation_link = eventData.ConfirmationLink,
            terms_link = termsLink
        };

        // Relativ sti oppdatert til SystemActions/
        var htmlBody = await templateRenderService.RenderTemplateAsync("SystemActions/Confirmation14DaysReminder", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Kontoen din er midlertidig sperret - Kjøkkenhylla",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}