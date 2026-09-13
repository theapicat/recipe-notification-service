using Contracts.Events.SystemActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.Interfaces;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors.SystemActions;

public class Confirmation14DaysReminderProcessor(
    ITemplateRenderService templateRenderService,
    IPendingEmailService pendingEmailService,
    IOptions<AppSettings> appSettings,
    ILogger<Confirmation14DaysReminderProcessor> logger) : IEventProcessor<Confirmation14DaysReminderEvent>
{
    public async Task ProcessAsync(Confirmation14DaysReminderEvent eventData,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler varsel om sperret konto (14 dager) for {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            confirmation_link = eventData.ConfirmationLink,
            terms_link = appSettings.Value.GetTermsLink()
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync(
            "SystemActions/Confirmation14DaysReminder",
            templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            "Kontoen din er midlertidig sperret - Kjøkkenhylla",
            htmlBody,
            nameof(Confirmation14DaysReminderEvent),
            cancellationToken
        );
    }
}