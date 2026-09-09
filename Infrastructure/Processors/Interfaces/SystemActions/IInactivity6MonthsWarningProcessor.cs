using Contracts.Events.SystemActions;

namespace Infrastructure.Processors.Interfaces.SystemActions;

public interface IInactivity6MonthsWarningProcessor
{
    Task ProcessAsync(Inactivity6MonthsWarningEvent eventData, CancellationToken cancellationToken = default);
}