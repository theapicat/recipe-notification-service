using Contracts.Events;

namespace Infrastructure.Processors.Interfaces;

public interface IUserRegisteredProcessor
{
    Task ProcessAsync(UserRegisteredEvent eventData, CancellationToken cancellationToken = default);
}