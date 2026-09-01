using Infrastructure.EmailService.Configurations;
using Infrastructure.EmailService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.EmailService;

public class EmailSenderService(SmtpSettings settings, ILogger<EmailSenderService> logger) : IEmailSenderService
{
    public Task SendEmailAsync(string email, string subject, string message, CancellationToken token)
    {
        logger.LogInformation($"Sending email to {email} with subject {subject}...");
        return Task.CompletedTask;
    } 
}