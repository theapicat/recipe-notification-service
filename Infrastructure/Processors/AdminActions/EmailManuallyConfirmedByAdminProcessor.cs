using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.Interfaces.AdminActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors.AdminActions;

public class EmailManuallyConfirmedByAdminProcessor(
    ITemplateRenderService templateRenderService,
    IPendingEmailService pendingEmailService,
    IOptions<AppSettings> appSettings,
    ILogger<EmailManuallyConfirmedByAdminProcessor> logger) : IEmailManuallyConfirmedByAdminProcessor
{
    public async Task ProcessAsync(EmailManuallyConfirmedByAdminEvent eventData,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler bekreftelsesepost (manuell admin) for bruker {Email}", eventData.Email);

        var loginLink = $"{appSettings.Value.FrontendUrl.TrimEnd('/')}/login";

        var templateModel = new
        {
            name = eventData.Name,
            login_link = loginLink
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync(
            "AdminActions/EmailManuallyConfirmedByAdmin",
            templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            "E-postadressen din har blitt bekreftet av administrator",
            htmlBody,
            nameof(EmailManuallyConfirmedByAdminEvent),
            cancellationToken
        );
    }
}