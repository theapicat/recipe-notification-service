using Serilog;
using Microsoft.Extensions.Hosting;

namespace Service.Extensions;

public static class SerilogExtensions
{
    public static HostApplicationBuilder AddSerilogLogging(this HostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, config) =>
        {
            config.ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services);
        });

        return builder;
    }
}