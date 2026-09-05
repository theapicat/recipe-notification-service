using Contracts.Events.UserActions;

namespace Infrastructure.Processors.Interfaces.UserActions;

public interface IUserRegisteredProcessor
{
    Task ProcessAsync(UserRegisteredEvent eventData, CancellationToken cancellationToken = default);
}