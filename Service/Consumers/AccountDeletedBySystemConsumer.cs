using Contracts.Events;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers;

public class AccountDeletedBySystemConsumer(
    IAccountDeletedBySystemProcessor processor,
    ILogger<AccountDeletedBySystemConsumer> logger) : IConsumer<UserAccountDeletedBySystemEvent>
{
    public async Task Consume(ConsumeContext<UserAccountDeletedBySystemEvent> context)
    {
        logger.LogInformation("Mottok UserAccountDeletedBySystemEvent for e-post: {Email}", context.Message.Email);

        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}