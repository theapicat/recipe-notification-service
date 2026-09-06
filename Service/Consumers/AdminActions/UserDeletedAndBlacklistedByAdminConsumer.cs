using Contracts.Events.AdminActions;
using Infrastructure.Processors.Interfaces.AdminActions;
using MassTransit;

namespace Service.Consumers.AdminActions;

public class UserDeletedAndBlacklistedByAdminConsumer(
    IUserDeletedAndBlacklistedByAdminProcessor processor) : IConsumer<UserDeletedAndBlacklistedByAdminEvent>
{
    public async Task Consume(ConsumeContext<UserDeletedAndBlacklistedByAdminEvent> context)
    {
        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}