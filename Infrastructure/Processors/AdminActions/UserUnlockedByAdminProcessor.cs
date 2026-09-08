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
    IPendingEmailService pendingEmailService,
    IOptions<AppSettings> appSettings,
    ILogger<UserUnlockedByAdminProcessor> logger) : IUserUnlockedByAdminProcessor
{
    public async Task ProcessAsync(UserUnlockedByAdminEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler gjenåpningsnotifikasjon (admin) for {Email}", eventData.Email);

        var loginLink = $"{appSettings.Value.FrontendUrl.TrimEnd('/')}/login";

        var templateModel = new
        {
            name = eventData.Name,
            unlocked_at = eventData.UnlockedAt.ToString("dd.MM.yyyy HH:mm"),
            login_link = loginLink
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync("AdminActions/UserUnlockedByAdmin", templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            "Kontoen din hos Kjøkkenhylla har blitt gjenåpnet",
            htmlBody,
            nameof(UserUnlockedByAdminEvent),
            cancellationToken
        );
    }
}