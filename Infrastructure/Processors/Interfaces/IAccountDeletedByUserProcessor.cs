using Contracts.Events;

namespace Infrastructure.Processors.Interfaces;

public interface IAccountDeletedByUserProcessor
{
    Task ProcessAsync(UserAccountDeletedByUserEvent eventData, CancellationToken cancellationToken = default);
}