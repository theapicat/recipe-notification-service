using Infrastructure.EmailService;
using Infrastructure.EmailService.Configurations;
using Infrastructure.EmailService.Interfaces;

namespace Service.Extensions;

public static class SmtpExtensions
{
    public static IServiceCollection AddSmtpService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpSettings>(configuration.GetSection("smtpSettings"));
        services.AddTransient<IEmailSenderService, EmailSenderService>();
        return services;
    }
}