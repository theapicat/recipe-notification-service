using Contracts.Events.UserActions;

namespace Infrastructure.Processors.Interfaces.UserActions;

public interface IUserRegisteredWithGoogleProcessor
{
    Task ProcessAsync(UserRegisteredWithGoogleEvent eventData, CancellationToken cancellationToken = default);
}