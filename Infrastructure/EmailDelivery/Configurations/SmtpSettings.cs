namespace Infrastructure.EmailDelivery.Configurations;

public class SmtpSettings
{
    public string Host { get; set; }
    public int Port { get; set; } = 1025;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = false;
    public string DefaultSenderEmail { get; set; }
    public string DefaultSenderName { get; set; }
    public string AdminNotificationEmail { get; set; } 
}