using Persistence.Entities;

namespace Persistence.Repositories.Interfaces;

public interface IFailedNotificationRepository
{
    // Skrive & Slette
    Task AddAsync(FailedNotification notification, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task UpdateFailedAttemptAsync(Guid id, string errorMessage, CancellationToken cancellationToken = default);

    // Admin handling: Nullstill RetryCount til 0 for re-forsøk
    Task ResetRetryCountAsync(Guid id, CancellationToken cancellationToken = default);
    Task ResetRetryCountManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    // Henting for bakgrunnsjobben (Henter KUN de som har RetryCount < maxRetryCount, f.eks. < 5)
    Task<List<FailedNotification>> GetPendingBatchAsync(int maxRetryCount = 5, int batchSize = 50, CancellationToken cancellationToken = default);

    // Innsyn for Admin & Dashbord
    Task<long> GetPendingCountAsync(CancellationToken cancellationToken = default);
    Task<List<FailedNotification>> GetAllPendingAsync(CancellationToken cancellationToken = default);
}