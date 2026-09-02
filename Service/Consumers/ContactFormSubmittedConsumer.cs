using Contracts.Events;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers;

public class ContactFormSubmittedConsumer(
    IContactFormProcessor processor,
    ILogger<ContactFormSubmittedConsumer> logger) : IConsumer<ContactFormSubmittedEvent>
{
    public async Task Consume(ConsumeContext<ContactFormSubmittedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Mottok ContactFormSubmittedEvent for e-post: {Email}", message.Email);

        await processor.ProcessAsync(message, context.CancellationToken);
    }
}