using Contracts.Events.SystemActions;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers.SystemActions;

public class Inactivity6MonthsWarningConsumer(IEventProcessor<Inactivity6MonthsWarningEvent> processor)
    : IConsumer<Inactivity6MonthsWarningEvent>
{
    public async Task Consume(ConsumeContext<Inactivity6MonthsWarningEvent> context)
    {
        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}