using Contracts.Events;

namespace Infrastructure.Processors;

public interface IResendEmailConfirmationProcessor
{
    Task ProcessAsync(ResendEmailConfirmationRequestedEvent eventData, CancellationToken cancellationToken = default);
}