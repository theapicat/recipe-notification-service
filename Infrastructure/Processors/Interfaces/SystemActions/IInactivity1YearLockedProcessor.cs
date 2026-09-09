using Contracts.Events.SystemActions;

namespace Infrastructure.Processors.Interfaces.SystemActions;

public interface IInactivity1YearLockedProcessor
{
    Task ProcessAsync(Inactivity1YearLockedEvent eventData, CancellationToken cancellationToken = default);
}