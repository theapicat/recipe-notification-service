using Contracts.Events;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors;

public class UserRegisteredProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    ILogger<UserRegisteredProcessor> logger) : IUserRegisteredProcessor
{
    public async Task ProcessAsync(UserRegisteredEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender velkomst-epost til ny bruker {Email}", eventData.Email);

        var templateModel = new
        {
            Name = eventData.Name,
            ConfirmationLink = eventData.ConfirmationLink
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