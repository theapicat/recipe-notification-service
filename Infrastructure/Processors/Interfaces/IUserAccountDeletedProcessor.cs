using Contracts.Events;

namespace Infrastructure.Processors.Interfaces;

public interface IUserAccountDeletedProcessor
{
    Task ProcessAsync(UserAccountDeletedEvent eventData, CancellationToken cancellationToken = default);
}