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
    IEmailDeliveryService emailDelivery,
    IOptions<AppSettings> appSettings,
    ILogger<UserRegisteredWithGoogleProcessor> logger) : IUserRegisteredWithGoogleProcessor
{
    public async Task ProcessAsync(UserRegisteredWithGoogleEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender Google-velkomst-epost til ny bruker {Email}", eventData.Email);

        var frontendUrl = appSettings.Value.FrontendUrl.TrimEnd('/');
        var termsLink = $"{frontendUrl}/legal/terms";

        var templateModel = new
        {
            name = eventData.Name,
            frontend_url = frontendUrl,
            terms_link = termsLink
        };

        // Relativ sti oppdatert til UserActions/
        var htmlBody = await templateRenderService.RenderTemplateAsync("UserActions/UserRegisteredWithGoogleWelcome", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Velkommen til Kjøkkenhylla!",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}