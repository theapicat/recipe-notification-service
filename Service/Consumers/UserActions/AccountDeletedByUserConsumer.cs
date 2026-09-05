using Contracts.Events.UserActions;
using Infrastructure.Processors.Interfaces.UserActions;
using MassTransit;

namespace Service.Consumers.UserActions;

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