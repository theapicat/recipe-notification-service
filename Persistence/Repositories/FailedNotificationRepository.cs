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

    public async Task<List<FailedNotification>> GetPendingRetryAsync(int maxBatchSize = 50, CancellationToken cancellationToken = default)
    {
        var filter = Builders<FailedNotification>.Filter.And(
            Builders<FailedNotification>.Filter.Eq(x => x.IsProcessed, false),
            Builders<FailedNotification>.Filter.Lte(x => x.NextAttemptAt, DateTime.UtcNow)
        );

        return await _collection.Find(filter)
            .Limit(maxBatchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var update = Builders<FailedNotification>.Update
            .Set(x => x.IsProcessed, true)
            .Set(x => x.LastAttemptAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
    }

    public async Task UpdateFailedAttemptAsync(Guid id, string errorMessage, DateTime nextAttemptAt, CancellationToken cancellationToken = default)
    {
        var update = Builders<FailedNotification>.Update
            .Inc(x => x.RetryCount, 1)
            .Set(x => x.LastErrorMessage, errorMessage)
            .Set(x => x.LastAttemptAt, DateTime.UtcNow)
            .Set(x => x.NextAttemptAt, nextAttemptAt);

        await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
    }
}