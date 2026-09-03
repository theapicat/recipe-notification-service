using Contracts.Events;

namespace Infrastructure.Processors;

public interface IPasswordResetRequestedProcessor
{
    Task ProcessAsync(PasswordResetRequestedEvent eventData, CancellationToken cancellationToken = default);
}