using Infrastructure.Processors;
using Infrastructure.Processors.Interfaces;

namespace Service.Extensions;

public static class MailProcessorExtensions
{
    public static IServiceCollection AddMailProcessors(this IServiceCollection services)
    {
        services.AddTransient<IContactFormProcessor, ContactFormProcessor>();
        return services;
    }
}