using Contracts.Events.SystemActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.Interfaces.SystemActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors.SystemActions;

public class Inactivity1YearLockedProcessor(
    ITemplateRenderService templateRenderService,
    IPendingEmailService pendingEmailService,
    IOptions<AppSettings> appSettings,
    ILogger<Inactivity1YearLockedProcessor> logger) : IInactivity1YearLockedProcessor
{
    public async Task ProcessAsync(Inactivity1YearLockedEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler 1-års inaktivitets-sperrevarsel for {Email}", eventData.Email);

        var loginLink = $"{appSettings.Value.FrontendUrl.TrimEnd('/')}/login";

        var templateModel = new
        {
            name = eventData.Name,
            login_link = loginLink
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync(
            "SystemActions/Inactivity1YearLocked",
            templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            "Kontoen din hos Kjøkkenhylla er midlertidig deaktivert pga. inaktivitet",
            htmlBody,
            nameof(Inactivity1YearLockedEvent),
            cancellationToken
        );
    }
}