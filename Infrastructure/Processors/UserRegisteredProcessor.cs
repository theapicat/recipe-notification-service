using Contracts.Events;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.Interfaces;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors;

public class UserRegisteredProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    IOptions<AppSettings> appSettings,
    ILogger<UserRegisteredProcessor> logger) : IUserRegisteredProcessor
{
    public async Task ProcessAsync(UserRegisteredEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender velkomst-epost til ny bruker {Email}", eventData.Email);

        var frontendUrl = !string.IsNullOrWhiteSpace(appSettings.Value?.FrontendUrl) 
            ? appSettings.Value.FrontendUrl 
            : "http://localhost:3000";

        var termsLink = $"{frontendUrl.TrimEnd('/')}/terms";

        var templateModel = new
        {
            name = eventData.Name,
            confirmation_link = eventData.ConfirmationLink,
            terms_link = termsLink
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync("UserRegisteredWelcome", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Velkommen til Kjøkkenhylla! Bekreft din e-postadresse",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}