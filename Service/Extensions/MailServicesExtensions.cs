using Infrastructure.EmailDelivery;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.State;
using Infrastructure.State.Interfaces;
using Infrastructure.TemplateService;
using Infrastructure.TemplateService.Interfaces;

namespace Service.Extensions;

public static class MailServicesExtensions
{
    public static IServiceCollection AddMailServices(this IServiceCollection services)
    {
        services.AddScoped<IEmailDeliveryService, EmailDeliveryService>();
        services.AddTransient<ITemplateRenderService, TemplateRenderService>();

        services.AddSingleton<INotificationStateStore, NotificationStateStore>();
        services.AddHostedService<StateStoreInitializerHostedService>();
        services.AddScoped<IPendingEmailService, PendingEmailService>();
        
        return services;
    }
}