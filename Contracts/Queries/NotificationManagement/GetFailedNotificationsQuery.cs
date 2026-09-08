namespace Contracts.Queries.NotificationManagement;

public class GetFailedNotificationsQuery
{
}

public class FailedNotificationDto
{
    public Guid Id { get; set; }
    public required string RecipientEmail { get; set; }
    public required string Subject { get; set; }
    public required string HtmlBody { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string LastErrorMessage { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
}

public class GetFailedNotificationsResponse
{
    public List<FailedNotificationDto> Items { get; set; } = [];
}