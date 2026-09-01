using Contracts.Events;

namespace Infrastructure.Processors.Interfaces;

public interface IContactFormProcessor
{
    Task ProcessAsync(ContactFormSubmittedEvent eventData, CancellationToken cancellationToken = default);
}