using Contracts.Events.UserActions;
using Infrastructure.Processors.Interfaces.UserActions;
using MassTransit;

namespace Service.Consumers.UserActions;

public class PasswordChangedConsumer(
    IPasswordChangedProcessor processor,
    ILogger<PasswordChangedConsumer> logger) : IConsumer<PasswordChangedEvent>
{
    public async Task Consume(ConsumeContext<PasswordChangedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Mottok PasswordChangedEvent for e-post: {Email}", message.Email);

        await processor.ProcessAsync(message, context.CancellationToken);
    }
}