using Contracts.Events.UserActions;

namespace Infrastructure.Processors.Interfaces.UserActions;

public interface IResendEmailConfirmationProcessor
{
    Task ProcessAsync(ResendEmailConfirmationRequestedEvent eventData, CancellationToken cancellationToken = default);
}