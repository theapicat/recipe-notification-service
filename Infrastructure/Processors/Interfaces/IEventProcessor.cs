namespace Infrastructure.Processors.Interfaces;

public interface IEventProcessor<in TEvent>
{
    Task ProcessAsync(TEvent eventData, CancellationToken cancellationToken = default);
}
