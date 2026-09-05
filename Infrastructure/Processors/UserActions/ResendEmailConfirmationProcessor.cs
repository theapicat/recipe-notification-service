using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.Interfaces.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors.UserActions;

public class ResendEmailConfirmationProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    IOptions<AppSettings> appSettings,
    ILogger<ResendEmailConfirmationProcessor> logger) : IResendEmailConfirmationProcessor
{
    public async Task ProcessAsync(ResendEmailConfirmationRequestedEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender ny bekreftelses-epost til bruker {Email}", eventData.Email);

        var termsLink = $"{appSettings.Value.FrontendUrl.TrimEnd('/')}/legal/terms";

        var templateModel = new
        {
            name = eventData.Name,
            confirmation_link = eventData.ConfirmationLink,
            terms_link = termsLink
        };

        // Relativ sti oppdatert til UserActions/
        var htmlBody = await templateRenderService.RenderTemplateAsync("UserActions/ResendEmailConfirmation", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Bekreft din e-postadresse - Kjøkkenhylla",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}