using Persistence.Entities;

namespace Persistence.Repositories.Interfaces;

public interface IFailedNotificationRepository
{
    Task AddAsync(FailedNotification notification, CancellationToken cancellationToken = default);
    Task<List<FailedNotification>> GetPendingRetryAsync(int maxBatchSize = 50, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateFailedAttemptAsync(Guid id, string errorMessage, DateTime nextAttemptAt, CancellationToken cancellationToken = default);
}