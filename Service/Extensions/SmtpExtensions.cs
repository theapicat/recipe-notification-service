using Infrastructure.EmailDelivery.Configurations;

namespace Service.Extensions;

public static class SmtpExtensions
{
    public static IServiceCollection AddSmtpService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
        return services;
    }
}