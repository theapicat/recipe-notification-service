using Contracts.Events.UserActions;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers.UserActions;

public class UserRegisteredWithGoogleConsumer(
    IEventProcessor<UserRegisteredWithGoogleEvent> processor,
    ILogger<UserRegisteredWithGoogleConsumer> logger) : IConsumer<UserRegisteredWithGoogleEvent>
{
    public async Task Consume(ConsumeContext<UserRegisteredWithGoogleEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Mottok UserRegisteredWithGoogleEvent for e-post: {Email}", message.Email);

        await processor.ProcessAsync(message, context.CancellationToken);
    }
}