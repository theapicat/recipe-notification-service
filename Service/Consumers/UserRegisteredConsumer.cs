using Contracts.Events;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers;

public class UserRegisteredConsumer(
    IUserRegisteredProcessor processor) : IConsumer<UserRegisteredEvent>
{
    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var message = context.Message;
        await processor.ProcessAsync(message, context.CancellationToken);
    }
}