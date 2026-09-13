using Contracts.Events.SystemActions;
using Infrastructure.Processors.Interfaces;
using MassTransit;

namespace Service.Consumers.SystemActions;

public class AccountDeletedBySystemConsumer(
    IEventProcessor<UserAccountDeletedBySystemEvent> processor,
    ILogger<AccountDeletedBySystemConsumer> logger) : IConsumer<UserAccountDeletedBySystemEvent>
{
    public async Task Consume(ConsumeContext<UserAccountDeletedBySystemEvent> context)
    {
        logger.LogInformation("Mottok UserAccountDeletedBySystemEvent for e-post: {Email}", context.Message.Email);

        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}