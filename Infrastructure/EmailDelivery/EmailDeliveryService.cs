using Infrastructure.EmailDelivery.Configurations;
using Infrastructure.EmailDelivery.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.EmailDelivery;

public class EmailDeliveryService(IOptions<SmtpSettings> settings, ILogger<EmailDeliveryService> logger)
    : IEmailDeliveryService
{
    private readonly SmtpSettings _settings = settings.Value;

    public async Task SendEmailAsync(
        string to, 
        string subject, 
        string htmlBody, 
        string replyTo = null, 
        CancellationToken cancellationToken = default)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_settings.DefaultSenderName, _settings.DefaultSenderEmail));
        mimeMessage.To.Add(MailboxAddress.Parse(to));

        if (!string.IsNullOrWhiteSpace(replyTo))
        {
            mimeMessage.ReplyTo.Add(MailboxAddress.Parse(replyTo));
        }

        mimeMessage.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };
        mimeMessage.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            var secureSocketOptions = _settings.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(_settings.Host, _settings.Port, secureSocketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            }

            await client.SendAsync(mimeMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Feil ved utsending av e-post '{Subject}' til {To}", subject, to);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}