namespace Contracts.Commands.NotificationManagement;

public class DeleteFailedNotificationCommand
{
    public List<Guid> NotificationIds { get; set; } = [];
}