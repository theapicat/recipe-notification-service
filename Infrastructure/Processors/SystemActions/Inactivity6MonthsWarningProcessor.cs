using Contracts.Events.SystemActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.Interfaces.SystemActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors.SystemActions;

public class Inactivity6MonthsWarningProcessor(
    ITemplateRenderService templateRenderService,
    IPendingEmailService pendingEmailService,
    IOptions<AppSettings> appSettings,
    ILogger<Inactivity6MonthsWarningProcessor> logger) : IInactivity6MonthsWarningProcessor
{
    public async Task ProcessAsync(Inactivity6MonthsWarningEvent eventData,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler 6-måneders inaktivitetsvarsel for {Email}", eventData.Email);

        var loginLink = $"{appSettings.Value.FrontendUrl.TrimEnd('/')}/login";

        var templateModel = new
        {
            name = eventData.Name,
            login_link = loginLink
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync(
            "SystemActions/Inactivity6MonthsWarning",
            templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            "Vi savner deg på Kjøkkenhylla!",
            htmlBody,
            nameof(Inactivity6MonthsWarningEvent),
            cancellationToken
        );
    }
}