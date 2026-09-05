using Contracts.Events.UserActions;
using Infrastructure.Processors.Interfaces.UserActions;
using MassTransit;

namespace Service.Consumers.UserActions;

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