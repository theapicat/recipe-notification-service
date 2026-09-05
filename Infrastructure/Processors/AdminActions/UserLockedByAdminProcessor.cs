using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces.AdminActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors.AdminActions;

public class UserLockedByAdminProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    ILogger<UserLockedByAdminProcessor> logger) : IUserLockedByAdminProcessor
{
    public async Task ProcessAsync(UserLockedByAdminEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender sperrenotifikasjon (admin) til {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            reason_details = eventData.ReasonDetails,
            locked_at = eventData.LockedAt.ToString("dd.MM.yyyy HH:mm")
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync("AdminActions/UserLockedByAdmin", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Kontoen din hos Kjøkkenhylla har blitt sperret",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}