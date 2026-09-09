using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors.UserActions;

public class PasswordChangedProcessor(
    ITemplateRenderService templateRenderService,
    IPendingEmailService pendingEmailService,
    ILogger<PasswordChangedProcessor> logger) : IPasswordChangedProcessor
{
    public async Task ProcessAsync(PasswordChangedEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Behandler sikkerhetsvarsel om endret passord for {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            changed_at = eventData.ChangedAt.ToString("dd.MM.yyyy HH:mm"),
            device_info = eventData.DeviceInfo,
            ip_address = eventData.IpAddress
        };

        var htmlBody =
            await templateRenderService.RenderTemplateAsync("UserActions/PasswordChangedSecurityNotice", templateModel);

        await pendingEmailService.ProcessEmailWithRetryAsync(
            eventData.Email,
            "Sikkerhetsvarsel: Passordet ditt har blitt endret",
            htmlBody,
            nameof(PasswordChangedEvent),
            cancellationToken
        );
    }
}