using Infrastructure.Options;
using Service.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.AddSerilogLogging();

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.SectionName));

builder.Services.AddMassTransitServices(builder.Configuration);
builder.Services.AddSmtpService(builder.Configuration);
builder.Services.AddMailServices();
builder.Services.AddNotificationProcessors();

var host = builder.Build();
host.Run();