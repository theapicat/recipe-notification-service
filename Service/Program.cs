using Service;
using Service.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransitServices(builder.Configuration);

var host = builder.Build();
host.Run();