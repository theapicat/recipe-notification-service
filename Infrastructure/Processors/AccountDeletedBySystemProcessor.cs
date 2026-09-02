using Contracts.Events;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.Interfaces;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Processors;

public class AccountDeletedBySystemProcessor(
    ITemplateRenderService templateRenderService,
    IEmailDeliveryService emailDelivery,
    IOptions<AppSettings> appSettings,
    ILogger<AccountDeletedBySystemProcessor> logger) : IAccountDeletedBySystemProcessor
{
    public async Task ProcessAsync(UserAccountDeletedBySystemEvent eventData, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sender system-slettevarsel til {Email}", eventData.Email);

        var termsLink = $"{appSettings.Value.FrontendUrl.TrimEnd('/')}/terms";

        var templateModel = new
        {
            name = eventData.Name,
            deletion_reason = eventData.DeletionReason,
            terms_link = termsLink
        };

        var htmlBody = await templateRenderService.RenderTemplateAsync("AccountDeletedBySystem", templateModel);

        await emailDelivery.SendEmailAsync(
            to: eventData.Email,
            subject: "Din konto hos Kjøkkenhylla er slettet",
            htmlBody: htmlBody,
            cancellationToken: cancellationToken
        );
    }
}