using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces.AdminActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors.AdminActions;

public class AdminCustomEmailRequestedProcessor(
    ITemplateRenderService templateRenderService,
    IPendingEmailService pendingEmailService,
    ILogger<AdminCustomEmailRequestedProcessor> logger) : IAdminCustomEmailRequestedProcessor
{
    public async Task ProcessAsync(AdminCustomEmailRequestedEvent eventData,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler e-post fra admin til bruker {Email} med emne '{Subject}'", eventData.Email, eventData.Subject);

        var templateModel = new
        {
            name = eventData.Name,
            subject = eventData.Subject,
            message = eventData.Message
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync("AdminActions/AdminCustomEmail", templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            eventData.Subject,
            htmlBody,
            nameof(AdminCustomEmailRequestedEvent),
            cancellationToken
        );
    }
}