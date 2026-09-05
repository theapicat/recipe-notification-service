using Contracts.Events.AdminActions;

namespace Infrastructure.Processors.Interfaces.AdminActions;

public interface IUserUpdatedByAdminProcessor
{
    Task ProcessAsync(UserUpdatedByAdminEvent eventData, CancellationToken cancellationToken = default);
}