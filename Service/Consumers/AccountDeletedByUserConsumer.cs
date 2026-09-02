using Contracts.Events;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers;

public class AccountDeletedByUserConsumer(
    IAccountDeletedByUserProcessor processor,
    ILogger<AccountDeletedByUserConsumer> logger) : IConsumer<UserAccountDeletedByUserEvent>
{
    public async Task Consume(ConsumeContext<UserAccountDeletedByUserEvent> context)
    {
        logger.LogInformation("Mottok UserAccountDeletedByUserEvent for e-post: {Email}", context.Message.Email);

        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}