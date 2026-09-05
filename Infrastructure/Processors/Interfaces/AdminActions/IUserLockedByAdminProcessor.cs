using Contracts.Events.AdminActions;

namespace Infrastructure.Processors.Interfaces.AdminActions;

public interface IUserLockedByAdminProcessor
{
    Task ProcessAsync(UserLockedByAdminEvent eventData, CancellationToken cancellationToken = default);
}