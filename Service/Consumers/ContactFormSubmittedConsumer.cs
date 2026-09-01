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

        logger.LogInformation(
            "[RabbitMQ] Mottok kontaktskjema fra {Name} ({Email}) | Emne: '{Subject}'",
            message.Name,
            message.Email,
            message.Subject
        );

        // Kaller prosessoren og sender med MassTransit sitt CancellationToken
        await processor.ProcessAsync(message, context.CancellationToken);
    }
}