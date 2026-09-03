using Contracts.Events;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers;

public class UserRegisteredWithGoogleConsumer(
    IUserRegisteredWithGoogleProcessor processor,
    ILogger<UserRegisteredWithGoogleConsumer> logger) : IConsumer<UserRegisteredWithGoogleEvent>
{
    public async Task Consume(ConsumeContext<UserRegisteredWithGoogleEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Mottok UserRegisteredWithGoogleEvent for e-post: {Email}", message.Email);

        await processor.ProcessAsync(message, context.CancellationToken);
    }
}