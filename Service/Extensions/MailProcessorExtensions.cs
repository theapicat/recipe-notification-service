using Contracts.Events.AdminActions;
using Contracts.Events.SystemActions;
using Contracts.Events.UserActions;
using Infrastructure.Processors.AdminActions;
using Infrastructure.Processors.Interfaces;
using Infrastructure.Processors.SystemActions;
using Infrastructure.Processors.UserActions;

namespace Service.Extensions;

public static class MailProcessorExtensions
{
    public static IServiceCollection AddNotificationProcessors(this IServiceCollection services)
    {
        // 1. User Actions
        services.AddTransient<IEventProcessor<UserAccountDeletedByUserEvent>, AccountDeletedByUserProcessor>();
        services.AddTransient<IEventProcessor<ContactFormSubmittedEvent>, ContactFormProcessor>();
        services.AddTransient<IEventProcessor<PasswordChangedEvent>, PasswordChangedProcessor>();
        services.AddTransient<IEventProcessor<PasswordResetRequestedEvent>, PasswordResetRequestedProcessor>();
        services.AddTransient<IEventProcessor<ResendEmailConfirmationRequestedEvent>, ResendEmailConfirmationProcessor>();
        services.AddTransient<IEventProcessor<UserRegisteredEvent>, UserRegisteredProcessor>();
        services.AddTransient<IEventProcessor<UserRegisteredWithGoogleEvent>, UserRegisteredWithGoogleProcessor>();

        // 2. System Actions
        services.AddTransient<IEventProcessor<UserAccountDeletedBySystemEvent>, AccountDeletedBySystemProcessor>();
        services.AddTransient<IEventProcessor<Confirmation7DaysReminderEvent>, Confirmation7DaysReminderProcessor>();
        services.AddTransient<IEventProcessor<Confirmation14DaysReminderEvent>, Confirmation14DaysReminderProcessor>();
        services.AddTransient<IEventProcessor<Inactivity6MonthsWarningEvent>, Inactivity6MonthsWarningProcessor>();
        services.AddTransient<IEventProcessor<Inactivity1YearLockedEvent>, Inactivity1YearLockedProcessor>();

        // 3. Admin Actions
        services.AddTransient<IEventProcessor<AdminCustomEmailRequestedEvent>, AdminCustomEmailRequestedProcessor>();
        services.AddTransient<IEventProcessor<EmailManuallyConfirmedByAdminEvent>, EmailManuallyConfirmedByAdminProcessor>();
        services.AddTransient<IEventProcessor<UserAccountDeletedByAdminEvent>, UserAccountDeletedByAdminProcessor>();
        services.AddTransient<IEventProcessor<UserDeletedAndBlacklistedByAdminEvent>, UserDeletedAndBlacklistedByAdminProcessor>();
        services.AddTransient<IEventProcessor<UserLockedByAdminEvent>, UserLockedByAdminProcessor>();
        services.AddTransient<IEventProcessor<UserUnlockedByAdminEvent>, UserUnlockedByAdminProcessor>();
        services.AddTransient<IEventProcessor<UserUpdatedByAdminEvent>, UserUpdatedByAdminProcessor>();

        return services;
    }
}
