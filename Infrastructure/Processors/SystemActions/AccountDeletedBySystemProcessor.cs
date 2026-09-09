using Contracts.Events.SystemActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.Interfaces.SystemActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors.SystemActions;

public class AccountDeletedBySystemProcessor(
    ITemplateRenderService templateRenderService,
    IPendingEmailService pendingEmailService,
    IOptions<AppSettings> appSettings,
    ILogger<AccountDeletedBySystemProcessor> logger) : IAccountDeletedBySystemProcessor
{
    public async Task ProcessAsync(UserAccountDeletedBySystemEvent eventData,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler system-slettevarsel for {Email}", eventData.Email);

        var termsLink = $"{appSettings.Value.FrontendUrl.TrimEnd('/')}/legal/terms";

        var templateModel = new
        {
            name = eventData.Name,
            deletion_reason = eventData.DeletionReason,
            terms_link = termsLink
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync(
            "SystemActions/AccountDeletedBySystem",
            templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            "Din konto hos Kjøkkenhylla har blitt slettet",
            htmlBody,
            nameof(UserAccountDeletedBySystemEvent),
            cancellationToken
        );
    }
}