using MassTransit;
using Service.Consumers;

namespace Service.Extensions;

public static class MassTransitExtensions
{
    public static IServiceCollection AddMassTransitServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<AccountDeletedBySystemConsumer>();
            x.AddConsumer<AccountDeletedByUserConsumer>();
            x.AddConsumer<Confirmation7DaysReminderConsumer>();
            x.AddConsumer<Confirmation14DaysReminderConsumer>();
            x.AddConsumer<ContactFormSubmittedConsumer>();
            x.AddConsumer<PasswordChangedConsumer>();
            x.AddConsumer<PasswordResetRequestedConsumer>();
            x.AddConsumer<UserRegisteredConsumer>();
            x.AddConsumer<UserRegisteredWithGoogleConsumer>();
            
            x.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RabbitMQ:Host"] ?? "localhost";
                var port = ushort.Parse(configuration["RabbitMQ:Port"] ?? "5672");
                var virtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/";
                var username = configuration["RabbitMQ:Username"] ?? "rabbit_user";
                var password = configuration["RabbitMQ:Password"] ?? "rabbit_secure_password_dev";

                cfg.Host(host, port, virtualHost, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}