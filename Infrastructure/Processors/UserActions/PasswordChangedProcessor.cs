using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors.UserActions;

public class PasswordChangedProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    ILogger<PasswordChangedProcessor> logger) : IPasswordChangedProcessor
{
    public async Task ProcessAsync(PasswordChangedEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender sikkerhetsvarsel om endret passord til {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            changed_at = eventData.ChangedAt.ToString("dd.MM.yyyy HH:mm"),
            device_info = eventData.DeviceInfo,
            ip_address = eventData.IpAddress
        };

        // Relativ sti oppdatert til UserActions/
        var htmlBody = await templateRenderService.RenderTemplateAsync("UserActions/PasswordChangedSecurityNotice", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Sikkerhetsvarsel: Passordet ditt har blitt endret",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}