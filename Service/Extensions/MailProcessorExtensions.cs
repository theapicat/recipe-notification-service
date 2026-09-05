using Infrastructure.Processors;
using Infrastructure.Processors.AdminActions;
using Infrastructure.Processors.Interfaces;
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
        // Support
        services.AddTransient<IContactFormProcessor, ContactFormProcessor>();

        // Konto & Sikkerhet
        services.AddTransient<IAccountDeletedBySystemProcessor, AccountDeletedBySystemProcessor>();
        services.AddTransient<IAccountDeletedByUserProcessor, AccountDeletedByUserProcessor>();
        
        services.AddTransient<IConfirmation7DaysReminderProcessor, Confirmation7DaysReminderProcessor>();
        services.AddTransient<IConfirmation14DaysReminderProcessor,  Confirmation14DaysReminderProcessor>();
        services.AddTransient<IResendEmailConfirmationProcessor, ResendEmailConfirmationProcessor>();
        
        services.AddTransient<IUserRegisteredProcessor, UserRegisteredProcessor>();
        services.AddTransient<IUserRegisteredWithGoogleProcessor, UserRegisteredWithGoogleProcessor>();
        services.AddTransient<IPasswordChangedProcessor, PasswordChangedProcessor>();
        services.AddTransient<IPasswordResetRequestedProcessor, PasswordResetRequestedProcessor>();


        services.AddTransient<IEmailManuallyConfirmedByAdminProcessor, EmailManuallyConfirmedByAdminProcessor>();
        services.AddTransient<IUserAccountDeletedByAdminProcessor, UserAccountDeletedByAdminProcessor>();
        services.AddTransient<IUserLockedByAdminProcessor, UserLockedByAdminProcessor>();
        services.AddTransient<IUserUnlockedByAdminProcessor, UserUnlockedByAdminProcessor>();
        services.AddTransient<IUserUpdatedByAdminProcessor, UserUpdatedByAdminProcessor>();

        return services;
    }
}