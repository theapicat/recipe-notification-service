using Contracts.Events;

namespace Infrastructure.Processors.Interfaces;

public interface IAccountDeletedBySystemProcessor
{
    Task ProcessAsync(UserAccountDeletedBySystemEvent eventData, CancellationToken cancellationToken = default);
}