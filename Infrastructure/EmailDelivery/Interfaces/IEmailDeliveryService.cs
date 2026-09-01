namespace Infrastructure.EmailDelivery.Interfaces;

public interface IEmailDeliveryService
{
    Task SendEmailAsync(
        string to, 
        string subject, 
        string htmlBody, 
        string replyTo = null, 
        CancellationToken cancellationToken = default);
}