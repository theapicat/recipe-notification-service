namespace Infrastructure.EmailDelivery.Interfaces;

public interface IPendingEmailService
{
    Task ProcessEmailWithRetryAsync(
        string to,
        string subject,
        string htmlBody,
        string eventType,
        CancellationToken cancellationToken = default);
}