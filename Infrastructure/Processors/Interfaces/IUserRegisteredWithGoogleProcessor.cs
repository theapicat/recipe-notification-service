using Contracts.Events;

namespace Infrastructure.Processors.Interfaces;

public interface IUserRegisteredWithGoogleProcessor
{
    Task ProcessAsync(UserRegisteredWithGoogleEvent eventData, CancellationToken cancellationToken = default);
}