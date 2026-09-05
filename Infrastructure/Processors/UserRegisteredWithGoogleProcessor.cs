using Contracts.Events;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.Interfaces;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors;

public class UserRegisteredWithGoogleProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    IOptions<AppSettings> appSettings,
    ILogger<UserRegisteredWithGoogleProcessor> logger) : IUserRegisteredWithGoogleProcessor
{
    public async Task ProcessAsync(UserRegisteredWithGoogleEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender Google-velkomst-epost til ny bruker {Email}", eventData.Email);

        var frontendUrl = !string.IsNullOrWhiteSpace(appSettings.Value?.FrontendUrl) 
            ? appSettings.Value.FrontendUrl 
            : "http://localhost:3000";

        var termsLink = $"{frontendUrl.TrimEnd('/')}/legal/terms";

        var templateModel = new
        {
            name = eventData.Name,
            frontend_url = frontendUrl,
            terms_link = termsLink
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync("UserRegisteredWithGoogleWelcome", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Velkommen til Kjøkkenhylla!",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}