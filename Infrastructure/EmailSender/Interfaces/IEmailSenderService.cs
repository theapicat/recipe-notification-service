namespace Recipe.Notification.Infrastructure.EmailSender.Interfaces;

public interface IEmailSenderService
{
    Task SendEmailAsync(
        string to, 
        string subject, 
        string htmlBody, 
        string replyTo = null, 
        CancellationToken cancellationToken = default);
}