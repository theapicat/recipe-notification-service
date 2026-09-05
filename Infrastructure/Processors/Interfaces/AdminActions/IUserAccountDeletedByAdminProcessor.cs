using Contracts.Events.AdminActions;

namespace Infrastructure.Processors.Interfaces.AdminActions;

public interface IUserAccountDeletedByAdminProcessor
{
    Task ProcessAsync(UserAccountDeletedByAdminEvent eventData, CancellationToken cancellationToken = default);
}