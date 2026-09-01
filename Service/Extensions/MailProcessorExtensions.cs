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
        services.AddTransient<IUserRegisteredProcessor, UserRegisteredProcessor>();
        services.AddTransient<IPasswordChangedProcessor, PasswordChangedProcessor>();
        services.AddTransient<IUserAccountDeletedProcessor, UserAccountDeletedProcessor>();

        return services;
    }
}