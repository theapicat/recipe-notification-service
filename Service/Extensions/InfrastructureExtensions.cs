using Infrastructure.EmailDelivery;
using Infrastructure.EmailDelivery.Configurations;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.State;
using Infrastructure.State.Interfaces;
using Infrastructure.TemplateService;
using Infrastructure.TemplateService.Interfaces;
using Persistence.Configurations;
using Persistence.Repositories;
using Persistence.Repositories.Interfaces;

namespace Service.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Options & Konfigurasjon
        services.AddOptions<AppSettings>()
            .Bind(configuration.GetSection(AppSettings.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.FrontendUrl),
                "Kritisk konfigurasjonsfeil: 'AppSettings:FrontendUrl' må være angitt i appsettings.json eller som miljøvariabel.")
            .ValidateOnStart();

        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
        services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));

        // 2. Persistens & Databasetjenester
        services.AddSingleton<IFailedNotificationRepository, FailedNotificationRepository>();

        // 3. E-post, Mal- og Tilstandstjenester
        services.AddScoped<IEmailDeliveryService, EmailDeliveryService>();
        services.AddTransient<ITemplateRenderService, TemplateRenderService>();
        services.AddSingleton<INotificationStateStore, NotificationStateStore>();
        services.AddHostedService<StateStoreInitializerHostedService>();
        services.AddScoped<IPendingEmailService, PendingEmailService>();

        return services;
    }
}