using Persistence.Entities;

namespace Persistence.Repositories.Interfaces;

public interface IFailedNotificationRepository
{
    // Skrive & Slette
    Task AddAsync(FailedNotification notification, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    // Admin handling: Nullstill RetryCount til 0 for re-forsøk
    Task ResetRetryCountManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    // Oppdaterer et eksisterende dokument etter et mislykket re-forsøk (i stedet for å opprette et duplikat)
    Task MarkRetryFailedAsync(Guid id, string errorMessage, int retryCount, CancellationToken cancellationToken = default);

    // Innsyn for Admin & Dashbord
    Task<long> GetPendingCountAsync(CancellationToken cancellationToken = default);
    Task<List<FailedNotification>> GetAllPendingAsync(CancellationToken cancellationToken = default);
}