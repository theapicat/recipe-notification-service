using Contracts.Events.UserActions;

namespace Infrastructure.Processors.Interfaces.UserActions;

public interface IAccountDeletedByUserProcessor
{
    Task ProcessAsync(UserAccountDeletedByUserEvent eventData, CancellationToken cancellationToken = default);
}