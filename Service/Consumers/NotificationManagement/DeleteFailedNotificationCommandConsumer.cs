using Contracts.Commands.NotificationManagement;
using Infrastructure.State.Interfaces;
using MassTransit;
using Persistence.Repositories.Interfaces;

namespace Service.Consumers.NotificationManagement;

public class DeleteFailedNotificationCommandConsumer(
    IFailedNotificationRepository repository,
    INotificationStateStore stateStore,
    ILogger<DeleteFailedNotificationCommandConsumer> logger) 
    : IConsumer<DeleteFailedNotificationCommand>
{
    public async Task Consume(ConsumeContext<DeleteFailedNotificationCommand> context)
    {
        var ids = context.Message.NotificationIds;
        logger.LogInformation("Sletter {Count} feilede e-poster fra MongoDB.", ids.Count);

        if (ids.Count == 0) return;

        await repository.DeleteManyAsync(ids, context.CancellationToken);

        var remainingCount = await repository.GetPendingCountAsync(context.CancellationToken);
        if (remainingCount == 0)
        {
            stateStore.Reset();
            logger.LogInformation("MongoDB-bufferen er nå tom. Tilbakestilte NotificationStateStore.");
        }
    }
}