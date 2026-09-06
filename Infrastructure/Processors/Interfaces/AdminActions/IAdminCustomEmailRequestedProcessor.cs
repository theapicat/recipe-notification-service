using Contracts.Events.AdminActions;

namespace Infrastructure.Processors.Interfaces.AdminActions;

public interface IAdminCustomEmailRequestedProcessor
{
    Task ProcessAsync(AdminCustomEmailRequestedEvent eventData, CancellationToken cancellationToken = default);
}