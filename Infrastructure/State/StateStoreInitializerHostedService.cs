using Infrastructure.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Persistence.Repositories.Interfaces;

namespace Infrastructure.State;

public class StateStoreInitializerHostedService(
    IServiceProvider serviceProvider,
    INotificationStateStore stateStore,
    ILogger<StateStoreInitializerHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initialiserer NotificationStateStore mot MongoDB...");

        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IFailedNotificationRepository>();

        var pendingCount = await repository.GetPendingCountAsync(cancellationToken);

        if (pendingCount > 0)
        {
            logger.LogWarning("Oppdaget {Count} ubehandlede e-poster i MongoDB ved oppstart.", pendingCount);
            stateStore.SetPendingStatus(true);
        }
        else
        {
            logger.LogInformation("MongoDB-bufferen er tom ved oppstart.");
            stateStore.SetPendingStatus(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}