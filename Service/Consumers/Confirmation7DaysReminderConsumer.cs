using Contracts.Events;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers;

public class Confirmation7DaysReminderConsumer(
    IConfirmation7DaysReminderProcessor processor,
    ILogger<Confirmation7DaysReminderConsumer> logger) : IConsumer<Confirmation7DaysReminderEvent>
{
    public async Task Consume(ConsumeContext<Confirmation7DaysReminderEvent> context)
    {
        logger.LogInformation("Mottok Confirmation7DaysReminderEvent for e-post: {Email}", context.Message.Email);

        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}