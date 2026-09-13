using Contracts.Events.SystemActions;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers.SystemActions;

public class Inactivity1YearLockedConsumer(IEventProcessor<Inactivity1YearLockedEvent> processor)
    : IConsumer<Inactivity1YearLockedEvent>
{
    public async Task Consume(ConsumeContext<Inactivity1YearLockedEvent> context)
    {
        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}