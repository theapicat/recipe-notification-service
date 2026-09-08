using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Persistence.Configurations;
using Persistence.Entities;
using Persistence.Repositories.Interfaces;

namespace Persistence.Repositories;

public class FailedNotificationRepository : IFailedNotificationRepository
{
    private readonly IMongoCollection<FailedNotification> _collection;

    public FailedNotificationRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<FailedNotification>(settings.Value.FailedNotificationsCollection);
    }

    public async Task AddAsync(FailedNotification notification, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(notification, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _collection.DeleteOneAsync(x => x.Id == id, cancellationToken);
    }

    public async Task DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var filter = Builders<FailedNotification>.Filter.In(x => x.Id, ids);
        await _collection.DeleteManyAsync(filter, cancellationToken);
    }

    public async Task UpdateFailedAttemptAsync(Guid id, string errorMessage, CancellationToken cancellationToken = default)
    {
        var update = Builders<FailedNotification>.Update
            .Inc(x => x.RetryCount, 1)
            .Set(x => x.LastErrorMessage, errorMessage)
            .Set(x => x.LastAttemptAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
    }

    public async Task ResetRetryCountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var update = Builders<FailedNotification>.Update
            .Set(x => x.RetryCount, 0)
            .Set(x => x.LastErrorMessage, "Manuelt tilbakestilt av admin");

        await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
    }

    public async Task ResetRetryCountManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var filter = Builders<FailedNotification>.Filter.In(x => x.Id, ids);
        var update = Builders<FailedNotification>.Update
            .Set(x => x.RetryCount, 0)
            .Set(x => x.LastErrorMessage, "Manuelt tilbakestilt av admin");

        await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task<List<FailedNotification>> GetPendingBatchAsync(
        int maxRetryCount = 5,
        int batchSize = 50,
        CancellationToken cancellationToken = default)
    {
        // Henter KUN elementer hvor RetryCount er lavere enn grenseverdien
        var filter = Builders<FailedNotification>.Filter.Lt(x => x.RetryCount, maxRetryCount);

        return await _collection.Find(filter)
            .SortBy(x => x.CreatedAt)
            .Limit(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _collection.CountDocumentsAsync(FilterDefinition<FailedNotification>.Empty, cancellationToken: cancellationToken);
    }

    public async Task<List<FailedNotification>> GetAllPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _collection.Find(FilterDefinition<FailedNotification>.Empty)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}