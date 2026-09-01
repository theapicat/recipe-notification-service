using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Mail;
using Infrastructure.TemplateService;
using Infrastructure.TemplateService.Interfaces;

namespace Service.Extensions;

public static class MailServicesExtensions
{
    public static IServiceCollection AddMailServices(this IServiceCollection services)
    {
        services.AddTransient<IEmailDeliveryService, EmailDeliveryService>();
        services.AddTransient<ITemplateRenderService, TemplateRenderService>();
        return services;
    }
}