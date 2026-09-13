using Contracts.Events.SystemActions;
using Infrastructure.Processors.Interfaces;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Service.Consumers.SystemActions;
using Shouldly;

namespace Tests.Consumers.SystemActions;

public class Confirmation14DaysReminderConsumerTests
{
    private readonly ILogger<Confirmation14DaysReminderConsumer> _logger =
        Substitute.For<ILogger<Confirmation14DaysReminderConsumer>>();

    private readonly IEventProcessor<Confirmation14DaysReminderEvent> _processor =
        Substitute.For<IEventProcessor<Confirmation14DaysReminderEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<Confirmation14DaysReminderConsumer>(); })
            .AddSingleton(_processor)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new Confirmation14DaysReminderEvent
        {
            UserId = Guid.NewGuid(),
            Email = "locked@example.com",
            Name = "Ola Nordmann",
            ConfirmationLink = "https://kjokkenhylla.no/confirm?token=1414",
            LockedAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<Confirmation14DaysReminderEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<Confirmation14DaysReminderEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
