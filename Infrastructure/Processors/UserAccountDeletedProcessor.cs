using Contracts.Events;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors;

public class UserAccountDeletedProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    ILogger<UserAccountDeletedProcessor> logger) : IUserAccountDeletedProcessor
{
    public async Task ProcessAsync(UserAccountDeletedEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender slettebekreftelse til {Email}", eventData.Email);

        var templateModel = new
        {
            Name = eventData.Name,
            DeletedAt = eventData.DeletedAt.ToString("dd.MM.yyyy HH:mm")
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync("AccountSelfDeletedConfirmation", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Bekreftelse på sletting av konto",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}