using Contracts.Events.SystemActions;

namespace Infrastructure.Processors.Interfaces.SystemActions;

public interface IAccountDeletedBySystemProcessor
{
    Task ProcessAsync(UserAccountDeletedBySystemEvent eventData, CancellationToken cancellationToken = default);
}