using Contracts.Events.UserActions;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers.UserActions;

public class ContactFormSubmittedConsumer(
    IEventProcessor<ContactFormSubmittedEvent> processor,
    ILogger<ContactFormSubmittedConsumer> logger) : IConsumer<ContactFormSubmittedEvent>
{
    public async Task Consume(ConsumeContext<ContactFormSubmittedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Mottok ContactFormSubmittedEvent for e-post: {Email}", message.Email);

        await processor.ProcessAsync(message, context.CancellationToken);
    }
}