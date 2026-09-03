using Contracts.Events;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.Interfaces;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors;

public class PasswordResetRequestedProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    IOptions<AppSettings> appSettings,
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

        var htmlBody = await templateRenderService.RenderTemplateAsync("PasswordResetRequested", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Tilbakestill ditt passord på Kjøkkenhylla",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}