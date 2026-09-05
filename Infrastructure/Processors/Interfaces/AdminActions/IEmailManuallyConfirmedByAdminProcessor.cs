using Contracts.Events.AdminActions;

namespace Infrastructure.Processors.Interfaces.AdminActions;

public interface IEmailManuallyConfirmedByAdminProcessor
{
    Task ProcessAsync(EmailManuallyConfirmedByAdminEvent eventData, CancellationToken cancellationToken = default);
}