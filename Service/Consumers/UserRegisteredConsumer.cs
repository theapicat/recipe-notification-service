using Contracts.Events;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers;

public class UserRegisteredConsumer(
    IUserRegisteredProcessor processor,
    ILogger<UserRegisteredConsumer> logger) : IConsumer<UserRegisteredEvent>
{
    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Mottok UserRegisteredEvent for e-post: {Email}", message.Email);

        await processor.ProcessAsync(message, context.CancellationToken);
    }
}