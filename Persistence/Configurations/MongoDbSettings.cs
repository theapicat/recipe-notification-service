namespace Persistence.Configurations;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string FailedNotificationsCollection { get; set; } = "failed_notifications";
}