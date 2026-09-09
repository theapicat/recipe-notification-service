using Infrastructure.Processors.AdminActions;
using Infrastructure.Processors.Interfaces.AdminActions;
using Infrastructure.Processors.Interfaces.SystemActions;
using Infrastructure.Processors.Interfaces.UserActions;
using Infrastructure.Processors.SystemActions;
using Infrastructure.Processors.UserActions;

namespace Service.Extensions;

public static class MailProcessorExtensions
{
    public static IServiceCollection AddNotificationProcessors(this IServiceCollection services)
    {
        // 1. User Actions
        services.AddTransient<IAccountDeletedByUserProcessor, AccountDeletedByUserProcessor>();
        services.AddTransient<IContactFormProcessor, ContactFormProcessor>();
        services.AddTransient<IPasswordChangedProcessor, PasswordChangedProcessor>();
        services.AddTransient<IPasswordResetRequestedProcessor, PasswordResetRequestedProcessor>();
        services.AddTransient<IResendEmailConfirmationProcessor, ResendEmailConfirmationProcessor>();
        services.AddTransient<IUserRegisteredProcessor, UserRegisteredProcessor>();
        services.AddTransient<IUserRegisteredWithGoogleProcessor, UserRegisteredWithGoogleProcessor>();

        // 2. System Actions
        services.AddTransient<IAccountDeletedBySystemProcessor, AccountDeletedBySystemProcessor>();
        services.AddTransient<IConfirmation7DaysReminderProcessor, Confirmation7DaysReminderProcessor>();
        services.AddTransient<IConfirmation14DaysReminderProcessor, Confirmation14DaysReminderProcessor>();
        services.AddTransient<IInactivity6MonthsWarningProcessor, Inactivity6MonthsWarningProcessor>();
        services.AddTransient<IInactivity1YearLockedProcessor, Inactivity1YearLockedProcessor>();

        // 3. Admin Actions
        services.AddTransient<IAdminCustomEmailRequestedProcessor, AdminCustomEmailRequestedProcessor>();
        services.AddTransient<IEmailManuallyConfirmedByAdminProcessor, EmailManuallyConfirmedByAdminProcessor>();
        services.AddTransient<IUserAccountDeletedByAdminProcessor, UserAccountDeletedByAdminProcessor>();
        services.AddTransient<IUserDeletedAndBlacklistedByAdminProcessor, UserDeletedAndBlacklistedByAdminProcessor>();
        services.AddTransient<IUserLockedByAdminProcessor, UserLockedByAdminProcessor>();
        services.AddTransient<IUserUnlockedByAdminProcessor, UserUnlockedByAdminProcessor>();
        services.AddTransient<IUserUpdatedByAdminProcessor, UserUpdatedByAdminProcessor>();

        return services;
    }
}