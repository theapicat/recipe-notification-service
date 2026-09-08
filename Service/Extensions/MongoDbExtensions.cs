using Persistence.Configurations;
using Persistence.Repositories;
using Persistence.Repositories.Interfaces;

namespace Service.Extensions;

public static class MongoDbExtensions
{
    public static IServiceCollection AddMongoDbPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));
        services.AddSingleton<IFailedNotificationRepository, FailedNotificationRepository>();

        return services;
    }
}