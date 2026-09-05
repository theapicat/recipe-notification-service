using Contracts.Events.UserActions;

namespace Infrastructure.Processors.Interfaces.UserActions;

public interface IPasswordResetRequestedProcessor
{
    Task ProcessAsync(PasswordResetRequestedEvent eventData, CancellationToken cancellationToken = default);
}