using Contracts.Events.AdminActions;
using Infrastructure.Processors.Interfaces.AdminActions;
using MassTransit;

namespace Service.Consumers.AdminActions;

public class UserLockedByAdminConsumer(
    IUserLockedByAdminProcessor processor,
    ILogger<UserLockedByAdminConsumer> logger) : IConsumer<UserLockedByAdminEvent>
{
    public async Task Consume(ConsumeContext<UserLockedByAdminEvent> context)
    {
        logger.LogInformation("Mottok UserLockedByAdminEvent for bruker {UserId} ({Email})", 
            context.Message.UserId, context.Message.Email);

        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}