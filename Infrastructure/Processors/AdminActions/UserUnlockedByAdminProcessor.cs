using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.Interfaces.AdminActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors.AdminActions;

public class UserUnlockedByAdminProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    IOptions<AppSettings> appSettings,
    ILogger<UserUnlockedByAdminProcessor> logger) : IUserUnlockedByAdminProcessor
{
    public async Task ProcessAsync(UserUnlockedByAdminEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender gjenåpningsnotifikasjon (admin) til {Email}", eventData.Email);

        var loginLink = $"{appSettings.Value.FrontendUrl.TrimEnd('/')}/login";

        var templateModel = new
        {
            name = eventData.Name,
            unlocked_at = eventData.UnlockedAt.ToString("dd.MM.yyyy HH:mm"),
            login_link = loginLink
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync("AdminActions/UserUnlockedByAdmin", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Kontoen din hos Kjøkkenhylla har blitt gjenåpnet",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}