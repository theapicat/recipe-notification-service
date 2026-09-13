using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Extensions;
using Infrastructure.Processors.Interfaces;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors.AdminActions;

public class UserLockedByAdminProcessor(
    ITemplateRenderService templateRenderService,
    IPendingEmailService pendingEmailService,
    ILogger<UserLockedByAdminProcessor> logger) : IEventProcessor<UserLockedByAdminEvent>
{
    public async Task ProcessAsync(UserLockedByAdminEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler sperrenotifikasjon (admin) for {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            reason_details = eventData.ReasonDetails,
            locked_at = eventData.LockedAt.ToNorwegianDisplayFormat()
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync("AdminActions/UserLockedByAdmin", templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            "Kontoen din hos Kjøkkenhylla har blitt sperret",
            htmlBody,
            nameof(UserLockedByAdminEvent),
            cancellationToken
        );
    }
}