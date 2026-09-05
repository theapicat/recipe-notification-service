using Contracts.Events;
using Infrastructure.Processors;
using MassTransit;

namespace Service.Consumers;

public class ResendEmailConfirmationRequestedConsumer(
    IResendEmailConfirmationProcessor processor) : IConsumer<ResendEmailConfirmationRequestedEvent>
{
    public async Task Consume(ConsumeContext<ResendEmailConfirmationRequestedEvent> context)
    {
        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}