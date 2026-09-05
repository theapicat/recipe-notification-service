using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces.AdminActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors.AdminActions;

public class UserUpdatedByAdminProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    ILogger<UserUpdatedByAdminProcessor> logger) : IUserUpdatedByAdminProcessor
{
    public async Task ProcessAsync(UserUpdatedByAdminEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender oppdateringsvarsel (admin) til {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            old_email = eventData.OldEmail,
            new_email = eventData.NewEmail,
            updated_at = eventData.UpdatedAt.ToString("dd.MM.yyyy HH:mm")
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync("AdminActions/UserUpdatedByAdmin", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Profilinformasjonen din hos Kjøkkenhylla har blitt oppdatert",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}