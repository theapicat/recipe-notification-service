using Infrastructure.State.Interfaces;

namespace Infrastructure.State;

public class NotificationStateStore : INotificationStateStore
{
    private readonly object _lock = new();

    public bool HasPendingNotifications { get; private set; }
    public bool HasNotifiedAdmin { get; private set; }

    public void SetPendingStatus(bool hasPending)
    {
        lock (_lock)
        {
            HasPendingNotifications = hasPending;
            if (!hasPending) HasNotifiedAdmin = false;
        }
    }

    public void SetAdminNotified(bool notified)
    {
        lock (_lock)
        {
            HasNotifiedAdmin = notified;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            HasPendingNotifications = false;
            HasNotifiedAdmin = false;
        }
    }
}