
using Infrastructure.EmailSender.Configurations;
using Infrastructure.EmailTemplate.Interfaces;
using Recipe.Notification.Infrastructure.EmailSender.Interfaces;

namespace Service.Extensions;

public static class SmtpExtensions
{
    public static IServiceCollection AddSmtpService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
        services.AddTransient<IEmailSenderService, IEmailSenderService>();
        services.AddTransient<IEmailTemplateService, IEmailTemplateService>();
        return services;
    }
}