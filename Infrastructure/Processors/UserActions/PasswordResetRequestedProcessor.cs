using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors.UserActions;

public class PasswordResetRequestedProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    ILogger<PasswordResetRequestedProcessor> logger) : IPasswordResetRequestedProcessor
{
    public async Task ProcessAsync(PasswordResetRequestedEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender instruksjoner for tilbakestilling av passord til {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            reset_link = eventData.ResetLink
        };

        // Relativ sti oppdatert til UserActions/
        var htmlBody = await templateRenderService.RenderTemplateAsync("UserActions/PasswordResetRequested", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Tilbakestill ditt passord på Kjøkkenhylla",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}