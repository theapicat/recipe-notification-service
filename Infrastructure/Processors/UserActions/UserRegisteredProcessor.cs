using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.Interfaces.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors.UserActions;

public class UserRegisteredProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    IOptions<AppSettings> appSettings,
    ILogger<UserRegisteredProcessor> logger) : IUserRegisteredProcessor
{
    public async Task ProcessAsync(UserRegisteredEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender velkomst-epost til ny bruker {Email}", eventData.Email);

        var termsLink = $"{appSettings.Value.FrontendUrl.TrimEnd('/')}/legal/terms";

        var templateModel = new
        {
            name = eventData.Name,
            confirmation_link = eventData.ConfirmationLink,
            terms_link = termsLink
        };

        // Relativ sti oppdatert til UserActions/
        var htmlBody = await templateRenderService.RenderTemplateAsync("UserActions/UserRegisteredWelcome", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Velkommen til Kjøkkenhylla! Bekreft din e-postadresse",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}