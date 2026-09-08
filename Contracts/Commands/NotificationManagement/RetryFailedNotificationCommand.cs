namespace Contracts.Commands.NotificationManagement;

public class RetryFailedNotificationCommand
{
    public List<Guid> NotificationIds { get; set; } = [];
}