using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces.AdminActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors.AdminActions;

public class UserAccountDeletedByAdminProcessor(
    ITemplateRenderService templateRenderService,
    IPendingEmailService pendingEmailService,
    ILogger<UserAccountDeletedByAdminProcessor> logger) : IUserAccountDeletedByAdminProcessor
{
    public async Task ProcessAsync(UserAccountDeletedByAdminEvent eventData,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler slettemelding (admin) for {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            email = eventData.Email
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync(
            "AdminActions/UserAccountDeletedByAdmin", 
            templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            "Din brukerkonto hos Kjøkkenhylla har blitt slettet",
            htmlBody,
            nameof(UserAccountDeletedByAdminEvent),
            cancellationToken
        );
    }
}