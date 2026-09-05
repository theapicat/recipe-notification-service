using Contracts.Events.UserActions;
using Infrastructure.Processors.Interfaces.UserActions;
using MassTransit;

namespace Service.Consumers.UserActions;

public class ResendEmailConfirmationRequestedConsumer(
    IResendEmailConfirmationProcessor processor) : IConsumer<ResendEmailConfirmationRequestedEvent>
{
    public async Task Consume(ConsumeContext<ResendEmailConfirmationRequestedEvent> context)
    {
        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}