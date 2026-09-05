using Contracts.Events.SystemActions;

namespace Infrastructure.Processors.Interfaces.SystemActions;

public interface IConfirmation7DaysReminderProcessor
{
    Task ProcessAsync(Confirmation7DaysReminderEvent eventData, CancellationToken cancellationToken = default);
}