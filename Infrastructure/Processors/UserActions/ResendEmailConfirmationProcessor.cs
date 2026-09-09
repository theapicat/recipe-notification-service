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
    IPendingEmailService pendingEmailService,
    IOptions<AppSettings> appSettings,
    ILogger<ResendEmailConfirmationProcessor> logger) : IResendEmailConfirmationProcessor
{
    public async Task ProcessAsync(ResendEmailConfirmationRequestedEvent eventData,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler ny bekreftelses-epost for bruker {Email}", eventData.Email);

        var termsLink = $"{appSettings.Value.FrontendUrl.TrimEnd('/')}/legal/terms";

        var templateModel = new
        {
            name = eventData.Name,
            confirmation_link = eventData.ConfirmationLink,
            terms_link = termsLink
        };

        var htmlBody =
            await templateRenderService.RenderTemplateAsync("UserActions/ResendEmailConfirmation", templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            "Bekreft din e-postadresse - Kjøkkenhylla",
            htmlBody,
            nameof(ResendEmailConfirmationRequestedEvent),
            cancellationToken
        );
    }
}