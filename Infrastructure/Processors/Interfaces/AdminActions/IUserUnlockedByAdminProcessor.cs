using Contracts.Events.AdminActions;

namespace Infrastructure.Processors.Interfaces.AdminActions;

public interface IUserUnlockedByAdminProcessor
{
    Task ProcessAsync(UserUnlockedByAdminEvent eventData, CancellationToken cancellationToken = default);
}