namespace Infrastructure.EmailService.Interfaces;

public interface IEmailSenderService
{
    Task SendEmailAsync(string email, string subject, string message, CancellationToken token);
}