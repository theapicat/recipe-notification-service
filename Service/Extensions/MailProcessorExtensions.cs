using Infrastructure.Processors;
using Infrastructure.Processors.Interfaces;

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
        
        services.AddTransient<IUserRegisteredProcessor, UserRegisteredProcessor>();
        services.AddTransient<IPasswordChangedProcessor, PasswordChangedProcessor>();

        return services;
    }
}