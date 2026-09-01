using Contracts.Events;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors;

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
            Name = eventData.Name,
            ChangedAt = eventData.ChangedAt.ToString("dd.MM.yyyy HH:mm"),
            DeviceInfo = eventData.DeviceInfo,
            IpAddress = eventData.IpAddress
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync("PasswordChangedSecurityNotice", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Sikkerhetsvarsel: Passordet ditt har blitt endret",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}