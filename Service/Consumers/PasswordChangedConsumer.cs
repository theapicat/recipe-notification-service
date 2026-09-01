using Contracts.Events;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers;

public class PasswordChangedConsumer(
    IPasswordChangedProcessor processor) : IConsumer<PasswordChangedEvent>
{
    public async Task Consume(ConsumeContext<PasswordChangedEvent> context)
    {
        var message = context.Message;

        await processor.ProcessAsync(message, context.CancellationToken);
    }
}