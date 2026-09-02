using Contracts.Events;

namespace Infrastructure.Processors.Interfaces;

public interface IConfirmation14DaysReminderProcessor
{
    Task ProcessAsync(Confirmation14DaysReminderEvent eventData, CancellationToken cancellationToken = default);
}