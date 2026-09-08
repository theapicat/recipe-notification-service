using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Persistence.Entities;

public class FailedNotification
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string RecipientEmail { get; set; }
    public required string Subject { get; set; }
    public required string HtmlBody { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string LastErrorMessage { get; set; } = string.Empty;

    public int RetryCount { get; set; } = 0;
    public bool IsProcessed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
}