using Contracts.Events.UserActions;

namespace Infrastructure.Processors.Interfaces.UserActions;

public interface IPasswordChangedProcessor
{
    Task ProcessAsync(PasswordChangedEvent eventData, CancellationToken cancellationToken = default);
}