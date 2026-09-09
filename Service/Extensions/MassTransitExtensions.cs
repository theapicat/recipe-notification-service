using MassTransit;
using Service.Consumers.AdminActions;
using Service.Consumers.NotificationManagement;
using Service.Consumers.SystemActions;
using Service.Consumers.UserActions;

namespace Service.Extensions;

public static class MassTransitExtensions
{
    public static IServiceCollection AddMassTransitServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            // User Actions
            x.AddConsumer<AccountDeletedByUserConsumer>();
            x.AddConsumer<ContactFormSubmittedConsumer>();
            x.AddConsumer<PasswordChangedConsumer>();
            x.AddConsumer<PasswordResetRequestedConsumer>();
            x.AddConsumer<ResendEmailConfirmationRequestedConsumer>();
            x.AddConsumer<UserRegisteredConsumer>();
            x.AddConsumer<UserRegisteredWithGoogleConsumer>();

            // System Actions
            x.AddConsumer<AccountDeletedBySystemConsumer>();
            x.AddConsumer<Confirmation7DaysReminderConsumer>();
            x.AddConsumer<Confirmation14DaysReminderConsumer>();
            x.AddConsumer<Inactivity6MonthsWarningConsumer>();
            x.AddConsumer<Inactivity1YearLockedConsumer>();

            // Admin Actions
            x.AddConsumer<AdminCustomEmailRequestedConsumer>();
            x.AddConsumer<EmailManuallyConfirmedByAdminConsumer>();
            x.AddConsumer<UserAccountDeletedByAdminConsumer>();
            x.AddConsumer<UserDeletedAndBlacklistedByAdminConsumer>();
            x.AddConsumer<UserLockedByAdminConsumer>();
            x.AddConsumer<UserUnlockedByAdminConsumer>();
            x.AddConsumer<UserUpdatedByAdminConsumer>();

            // Notification Management
            x.AddConsumer<DeleteFailedNotificationCommandConsumer>();
            x.AddConsumer<GetFailedNotificationsConsumer>();
            x.AddConsumer<RetryFailedNotificationCommandConsumer>();

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