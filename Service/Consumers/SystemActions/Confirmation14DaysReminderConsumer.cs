using Contracts.Events.SystemActions;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers.SystemActions;

public class Confirmation14DaysReminderConsumer(
    IEventProcessor<Confirmation14DaysReminderEvent> processor,
    ILogger<Confirmation14DaysReminderConsumer> logger) : IConsumer<Confirmation14DaysReminderEvent>
{
    public async Task Consume(ConsumeContext<Confirmation14DaysReminderEvent> context)
    {
        logger.LogInformation("Mottok Confirmation14DaysReminderEvent for e-post: {Email}", context.Message.Email);

        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}