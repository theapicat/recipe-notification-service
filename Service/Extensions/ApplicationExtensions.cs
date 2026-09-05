using Infrastructure.Options;

namespace Service.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AppSettings>()
            .Bind(configuration.GetSection(AppSettings.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.FrontendUrl),
                "Kritisk konfigurasjonsfeil: 'AppSettings:FrontendUrl' må være angitt i appsettings.json eller som miljøvariabel.")
            .ValidateOnStart();
        return services;
    }
}