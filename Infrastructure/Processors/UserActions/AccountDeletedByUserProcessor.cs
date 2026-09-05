using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors.UserActions;

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

        // Relativ sti oppdatert til UserActions/
        var htmlBody = await templateRenderService.RenderTemplateAsync("UserActions/AccountDeletedByUser", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Bekreftelse på sletting av konto - Kjøkkenhylla",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}