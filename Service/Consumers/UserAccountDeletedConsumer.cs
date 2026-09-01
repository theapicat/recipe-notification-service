using Contracts.Events;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers;

public class UserAccountDeletedConsumer(
    IUserAccountDeletedProcessor processor) : IConsumer<UserAccountDeletedEvent>
{
    public async Task Consume(ConsumeContext<UserAccountDeletedEvent> context)
    {
        var message = context.Message;
        
        await processor.ProcessAsync(message, context.CancellationToken);

    }
}