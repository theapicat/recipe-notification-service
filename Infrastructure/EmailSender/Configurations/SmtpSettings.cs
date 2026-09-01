namespace Infrastructure.EmailSender.Configurations;

public class SmtpSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = false;
    public string DefaultSenderEmail { get; set; } = "noreply@kjokkenhylla.no";
    public string DefaultSenderName { get; set; } = "Kjøkkenhylla";
    public string AdminNotificationEmail { get; set; } = "contact@kjokkenhylla.no";
}