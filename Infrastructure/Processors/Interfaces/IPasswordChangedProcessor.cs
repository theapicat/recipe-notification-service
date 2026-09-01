using Contracts.Events;

namespace Infrastructure.Processors.Interfaces;

public interface IPasswordChangedProcessor
{
    Task ProcessAsync(PasswordChangedEvent eventData, CancellationToken cancellationToken = default);
}