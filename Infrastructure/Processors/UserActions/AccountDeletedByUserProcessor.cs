using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors.UserActions;

public class AccountDeletedByUserProcessor(
    ITemplateRenderService templateRenderService,
    IPendingEmailService pendingEmailService,
    ILogger<AccountDeletedByUserProcessor> logger) : IAccountDeletedByUserProcessor
{
    public async Task ProcessAsync(UserAccountDeletedByUserEvent eventData,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler bekreftelse på brukerstyrt sletting for {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            deleted_at = eventData.DeletedAt.ToString("dd.MM.yyyy HH:mm")
        };

        var htmlBody =
            await templateRenderService.RenderTemplateAsync("UserActions/AccountDeletedByUser", templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            "Bekreftelse på sletting av konto - Kjøkkenhylla",
            htmlBody,
            nameof(UserAccountDeletedByUserEvent),
            cancellationToken
        );
    }
}