using Contracts.Events;

namespace Infrastructure.Processors.Interfaces;

public interface IConfirmation7DaysReminderProcessor
{
    Task ProcessAsync(Confirmation7DaysReminderEvent eventData, CancellationToken cancellationToken = default);
}