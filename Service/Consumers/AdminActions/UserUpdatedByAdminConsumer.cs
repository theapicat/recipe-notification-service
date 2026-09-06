using Contracts.Events.AdminActions;
using Infrastructure.Processors.Interfaces.AdminActions;
using MassTransit;

namespace Service.Consumers.AdminActions;

public class UserUpdatedByAdminConsumer(
    IUserUpdatedByAdminProcessor processor,
    ILogger<UserUpdatedByAdminConsumer> logger) : IConsumer<UserUpdatedByAdminEvent>
{
    public async Task Consume(ConsumeContext<UserUpdatedByAdminEvent> context)
    {
        logger.LogInformation("Mottok UserUpdatedByAdminEvent for bruker {UserId} ({Email})",
            context.Message.UserId, context.Message.Email);

        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}