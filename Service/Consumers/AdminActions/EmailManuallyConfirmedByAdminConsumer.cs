using Contracts.Events.AdminActions;
using Infrastructure.Processors.Interfaces.AdminActions;
using MassTransit;

namespace Service.Consumers.AdminActions;

public class EmailManuallyConfirmedByAdminConsumer(
    IEmailManuallyConfirmedByAdminProcessor processor,
    ILogger<EmailManuallyConfirmedByAdminConsumer> logger) : IConsumer<EmailManuallyConfirmedByAdminEvent>
{
    public async Task Consume(ConsumeContext<EmailManuallyConfirmedByAdminEvent> context)
    {
        logger.LogInformation("Mottok EmailManuallyConfirmedByAdminEvent for bruker {UserId} ({Email})", 
            context.Message.UserId, context.Message.Email);

        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}