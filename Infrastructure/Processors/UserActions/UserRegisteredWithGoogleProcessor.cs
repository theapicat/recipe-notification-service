using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.Interfaces.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors.UserActions;

public class UserRegisteredWithGoogleProcessor(
    ITemplateRenderService templateRenderService,
    IPendingEmailService pendingEmailService,
    IOptions<AppSettings> appSettings,
    ILogger<UserRegisteredWithGoogleProcessor> logger) : IUserRegisteredWithGoogleProcessor
{
    public async Task ProcessAsync(UserRegisteredWithGoogleEvent eventData,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler Google-velkomst-epost for ny bruker {Email}", eventData.Email);

        var frontendUrl = appSettings.Value.FrontendUrl.TrimEnd('/');
        var termsLink = $"{frontendUrl}/legal/terms";

        var templateModel = new
        {
            name = eventData.Name,
            frontend_url = frontendUrl,
            terms_link = termsLink
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync(
            "UserActions/UserRegisteredWithGoogleWelcome",
            templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            "Velkommen til Kjøkkenhylla!",
            htmlBody,
            nameof(UserRegisteredWithGoogleEvent),
            cancellationToken
        );
    }
}