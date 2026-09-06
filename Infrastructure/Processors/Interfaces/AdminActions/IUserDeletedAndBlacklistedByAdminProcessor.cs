using Contracts.Events.AdminActions;

namespace Infrastructure.Processors.Interfaces.AdminActions;

public interface IUserDeletedAndBlacklistedByAdminProcessor
{
    Task ProcessAsync(UserDeletedAndBlacklistedByAdminEvent eventData, CancellationToken cancellationToken = default);
}