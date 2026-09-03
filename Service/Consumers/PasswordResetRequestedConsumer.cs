using Contracts.Events;
using Infrastructure.Processors;
using MassTransit;

namespace Service.Consumers;

public class PasswordResetRequestedConsumer(
    IPasswordResetRequestedProcessor processor,
    ILogger<PasswordResetRequestedConsumer> logger) : IConsumer<PasswordResetRequestedEvent>
{
    public async Task Consume(ConsumeContext<PasswordResetRequestedEvent> context)
    {
        logger.LogInformation("Mottok PasswordResetRequestedEvent for e-post: {Email}", context.Message.Email);
        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}