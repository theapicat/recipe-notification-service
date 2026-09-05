using Contracts.Events.AdminActions;
using Infrastructure.Processors.Interfaces.AdminActions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Service.Consumers.AdminActions;

public class UserUnlockedByAdminConsumer(
    IUserUnlockedByAdminProcessor processor,
    ILogger<UserUnlockedByAdminConsumer> logger) : IConsumer<UserUnlockedByAdminEvent>
{
    public async Task Consume(ConsumeContext<UserUnlockedByAdminEvent> context)
    {
        logger.LogInformation("Mottok UserUnlockedByAdminEvent for bruker {UserId} ({Email})", 
            context.Message.UserId, context.Message.Email);

        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}