using Contracts.Events;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors;

public class AccountDeletedByUserProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    ILogger<AccountDeletedByUserProcessor> logger) : IAccountDeletedByUserProcessor
{
    public async Task ProcessAsync(UserAccountDeletedByUserEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender bekreftelse på brukerstyrt sletting til {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            deleted_at = eventData.DeletedAt.ToString("dd.MM.yyyy HH:mm")
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync("AccountDeletedByUser", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Bekreftelse på sletting av konto - Kjøkkenhylla",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}