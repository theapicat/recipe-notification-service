using Contracts.Events.UserActions;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers.UserActions;

public class PasswordResetRequestedConsumer(
    IEventProcessor<PasswordResetRequestedEvent> processor,
    ILogger<PasswordResetRequestedConsumer> logger) : IConsumer<PasswordResetRequestedEvent>
{
    public async Task Consume(ConsumeContext<PasswordResetRequestedEvent> context)
    {
        logger.LogInformation("Mottok PasswordResetRequestedEvent for e-post: {Email}", context.Message.Email);
        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}