using Contracts.Events.UserActions;

namespace Infrastructure.Processors.Interfaces.UserActions;

public interface IContactFormProcessor
{
    Task ProcessAsync(ContactFormSubmittedEvent eventData, CancellationToken cancellationToken = default);
}