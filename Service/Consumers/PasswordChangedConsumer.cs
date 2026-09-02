using Contracts.Events;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers;

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