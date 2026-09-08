namespace Infrastructure.State.Interfaces;

public interface INotificationStateStore
{
    bool HasPendingNotifications { get; }
    bool HasNotifiedAdmin { get; }
    void SetPendingStatus(bool hasPending);
    void SetAdminNotified(bool notified);
    void Reset();
}