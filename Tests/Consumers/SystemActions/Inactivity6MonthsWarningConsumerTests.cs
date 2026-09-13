using Contracts.Events.SystemActions;
using Infrastructure.Processors.Interfaces;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Service.Consumers.SystemActions;
using Shouldly;

namespace Tests.Consumers.SystemActions;

public class Inactivity6MonthsWarningConsumerTests
{
    private readonly IEventProcessor<Inactivity6MonthsWarningEvent> _processor =
        Substitute.For<IEventProcessor<Inactivity6MonthsWarningEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<Inactivity6MonthsWarningConsumer>(); })
            .AddSingleton(_processor)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new Inactivity6MonthsWarningEvent
        {
            UserId = Guid.NewGuid(),
            Email = "inactive@example.com",
            Name = "Kari Nordmann",
            WarnedAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<Inactivity6MonthsWarningEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<Inactivity6MonthsWarningEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
