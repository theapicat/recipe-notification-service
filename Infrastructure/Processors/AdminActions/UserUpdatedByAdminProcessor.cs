using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Extensions;
using Infrastructure.Processors.Interfaces;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors.AdminActions;

public class UserUpdatedByAdminProcessor(
    ITemplateRenderService templateRenderService,
    IPendingEmailService pendingEmailService,
    ILogger<UserUpdatedByAdminProcessor> logger) : IEventProcessor<UserUpdatedByAdminEvent>
{
    public async Task ProcessAsync(UserUpdatedByAdminEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler oppdateringsvarsel (admin) for {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            old_email = eventData.OldEmail,
            new_email = eventData.NewEmail,
            updated_at = eventData.UpdatedAt.ToNorwegianDisplayFormat()
        };

        var htmlBody =
            await templateRenderService.RenderTemplateAsync("AdminActions/UserUpdatedByAdmin", templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            "Profilinformasjonen din hos Kjøkkenhylla har blitt oppdatert",
            htmlBody,
            nameof(UserUpdatedByAdminEvent),
            cancellationToken
        );
    }
}