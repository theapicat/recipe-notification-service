namespace Infrastructure.EmailDelivery.Interfaces;

public interface IPendingEmailService
{
    // Returnerer true kun ved faktisk levert e-post. Ved re-forsøk av et allerede bufret dokument, send
    // med existingNotificationId: dokumentet slettes da kun ved suksess, og oppdateres (ikke dupliseres)
    // i MongoDB dersom re-forsøket feiler på nytt.
    Task<bool> ProcessEmailWithRetryAsync(
        string to,
        string subject,
        string htmlBody,
        string eventType,
        CancellationToken cancellationToken = default,
        Guid? existingNotificationId = null);
}