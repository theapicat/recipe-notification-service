using Contracts.Commands.NotificationManagement;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.State.Interfaces;
using MassTransit;
using Persistence.Repositories.Interfaces;

namespace Service.Consumers.NotificationManagement;

public class RetryFailedNotificationCommandConsumer(
    IFailedNotificationRepository repository,
    IPendingEmailService pendingEmailService,
    INotificationStateStore stateStore,
    ILogger<RetryFailedNotificationCommandConsumer> logger)
    : IConsumer<RetryFailedNotificationCommand>
{
    public async Task Consume(ConsumeContext<RetryFailedNotificationCommand> context)
    {
        var ids = context.Message.NotificationIds;
        logger.LogInformation("Mottok kommando om re-forsøk for {Count} feilede e-poster.", ids.Count);

        if (ids.Count == 0) return;

        await repository.ResetRetryCountManyAsync(ids, context.CancellationToken);

        var pendingEmails = await repository.GetAllPendingAsync(context.CancellationToken);
        var emailsToRetry = pendingEmails.Where(x => ids.Contains(x.Id)).ToList();

        foreach (var email in emailsToRetry)
        {
            await pendingEmailService.ProcessEmailWithRetryAsync(
                email.RecipientEmail,
                email.Subject,
                email.HtmlBody,
                email.EventType,
                context.CancellationToken);

            await repository.DeleteAsync(email.Id, context.CancellationToken);
        }

        var remainingCount = await repository.GetPendingCountAsync(context.CancellationToken);
        if (remainingCount == 0)
        {
            stateStore.Reset();
            logger.LogInformation("Alle feilede e-poster er behandlet. Tilbakestilte NotificationStateStore.");
        }
    }
}