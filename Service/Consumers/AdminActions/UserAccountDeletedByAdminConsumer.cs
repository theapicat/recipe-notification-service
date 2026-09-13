using Contracts.Events.AdminActions;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers.AdminActions;

public class UserAccountDeletedByAdminConsumer(
    IEventProcessor<UserAccountDeletedByAdminEvent> processor,
    ILogger<UserAccountDeletedByAdminConsumer> logger) : IConsumer<UserAccountDeletedByAdminEvent>
{
    public async Task Consume(ConsumeContext<UserAccountDeletedByAdminEvent> context)
    {
        logger.LogInformation("Mottok UserAccountDeletedByAdminEvent for bruker {UserId} ({Email})",
            context.Message.UserId, context.Message.Email);

        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}