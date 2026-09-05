using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces.AdminActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors.AdminActions;

public class UserAccountDeletedByAdminProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    ILogger<UserAccountDeletedByAdminProcessor> logger) : IUserAccountDeletedByAdminProcessor
{
    public async Task ProcessAsync(UserAccountDeletedByAdminEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender slettemelding (admin) til {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            email = eventData.Email
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync("AdminActions/UserAccountDeletedByAdmin", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Din brukerkonto hos Kjøkkenhylla har blitt slettet",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}