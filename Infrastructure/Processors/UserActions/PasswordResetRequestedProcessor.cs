using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors.UserActions;

public class PasswordResetRequestedProcessor(
    ITemplateRenderService templateRenderService,
    IPendingEmailService pendingEmailService,
    ILogger<PasswordResetRequestedProcessor> logger) : IPasswordResetRequestedProcessor
{
    public async Task ProcessAsync(PasswordResetRequestedEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler instruksjoner for tilbakestilling av passord for {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            reset_link = eventData.ResetLink
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync("UserActions/PasswordResetRequested", templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            "Tilbakestill ditt passord på Kjøkkenhylla",
            htmlBody,
            nameof(PasswordResetRequestedEvent),
            cancellationToken
        );
    }
}