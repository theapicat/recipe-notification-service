using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.Interfaces.AdminActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Processors.AdminActions;

public class UserDeletedAndBlacklistedByAdminProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    ILogger<UserDeletedAndBlacklistedByAdminProcessor> logger) : IUserDeletedAndBlacklistedByAdminProcessor
{
    public async Task ProcessAsync(UserDeletedAndBlacklistedByAdminEvent eventData,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender slette- og svartelistingsmelding (admin) til {Email}", eventData.Email);

        var templateModel = new
        {
            name = eventData.Name,
            email = eventData.Email,
            reason = eventData.Reason
        };

        var htmlBody =
            await templateRenderService.RenderTemplateAsync("AdminActions/UserDeletedAndBlacklistedByAdmin",
                templateModel);

        await emailDelivery.SendEmailAsync(
            eventData.Email,
            "Din brukerkonto hos Kjøkkenhylla har blitt slettet og utestengt",
            htmlBody,
            cancellationToken: cancellationToken
        );
    }
}