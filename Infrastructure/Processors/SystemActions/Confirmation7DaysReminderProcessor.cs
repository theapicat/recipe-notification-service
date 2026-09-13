using Contracts.Events.SystemActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.Interfaces;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors.SystemActions;

public class Confirmation7DaysReminderProcessor(
    ITemplateRenderService templateRenderService,
    IPendingEmailService pendingEmailService,
    IOptions<AppSettings> appSettings,
    ILogger<Confirmation7DaysReminderProcessor> logger) : IEventProcessor<Confirmation7DaysReminderEvent>
{
    public async Task ProcessAsync(Confirmation7DaysReminderEvent eventData,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler 7-dagers påminnelse om e-postbekreftelse for {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            confirmation_link = eventData.ConfirmationLink,
            terms_link = appSettings.Value.GetTermsLink()
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync(
            "SystemActions/Confirmation7DaysReminder",
            templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            "Påminnelse: Bekreft din e-postadresse - Kjøkkenhylla",
            htmlBody,
            nameof(Confirmation7DaysReminderEvent),
            cancellationToken
        );
    }
}