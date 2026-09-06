using Contracts.Events.AdminActions;
using Infrastructure.Processors.Interfaces.AdminActions;
using MassTransit;

namespace Service.Consumers.AdminActions;

public class AdminCustomEmailRequestedConsumer(
    IAdminCustomEmailRequestedProcessor processor,
    ILogger<AdminCustomEmailRequestedConsumer> logger) : IConsumer<AdminCustomEmailRequestedEvent>
{
    public async Task Consume(ConsumeContext<AdminCustomEmailRequestedEvent> context)
    {
        logger.LogInformation("Mottok AdminCustomEmailRequestedEvent for UserId {UserId}", context.Message.UserId);
        
        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}