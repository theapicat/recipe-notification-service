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
    IEmailDeliveryService emailDelivery,
    IOptions<AppSettings> appSettings,
    ILogger<EmailManuallyConfirmedByAdminProcessor> logger) : IEmailManuallyConfirmedByAdminProcessor
{
    public async Task ProcessAsync(EmailManuallyConfirmedByAdminEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender bekreftelsesepost (manuell admin) til bruker {Email}", eventData.Email);

        var loginLink = $"{appSettings.Value.FrontendUrl.TrimEnd('/')}/login";

        var templateModel = new
        {
            name = eventData.Name,
            login_link = loginLink
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync("AdminActions/EmailManuallyConfirmedByAdmin", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "E-postadressen din har blitt bekreftet av administrator",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}