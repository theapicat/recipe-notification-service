using Contracts.Events.SystemActions;

namespace Infrastructure.Processors.Interfaces.SystemActions;

public interface IConfirmation14DaysReminderProcessor
{
    Task ProcessAsync(Confirmation14DaysReminderEvent eventData, CancellationToken cancellationToken = default);
}