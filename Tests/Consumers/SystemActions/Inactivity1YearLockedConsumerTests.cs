using Contracts.Events.SystemActions;
using Infrastructure.Processors.Interfaces;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Service.Consumers.SystemActions;
using Shouldly;

namespace Tests.Consumers.SystemActions;

public class Inactivity1YearLockedConsumerTests
{
    private readonly IEventProcessor<Inactivity1YearLockedEvent> _processor =
        Substitute.For<IEventProcessor<Inactivity1YearLockedEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<Inactivity1YearLockedConsumer>(); })
            .AddSingleton(_processor)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new Inactivity1YearLockedEvent
        {
            UserId = Guid.NewGuid(),
            Email = "inactive1year@example.com",
            Name = "Ola Nordmann",
            LockedAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<Inactivity1YearLockedEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<Inactivity1YearLockedEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
