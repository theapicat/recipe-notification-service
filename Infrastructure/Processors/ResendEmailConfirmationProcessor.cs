using Contracts.Events;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors;

public class ResendEmailConfirmationProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    IOptions<AppSettings> appSettings,
    ILogger<ResendEmailConfirmationProcessor> logger) : IResendEmailConfirmationProcessor
{
    public async Task ProcessAsync(ResendEmailConfirmationRequestedEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender ny bekreftelses-epost til bruker {Email}", eventData.Email);

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

        var htmlBody = await templateRenderService.RenderTemplateAsync("ResendEmailConfirmation", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Bekreft din e-postadresse - Kjøkkenhylla",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}