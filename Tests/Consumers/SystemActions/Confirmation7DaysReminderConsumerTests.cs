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

public class Confirmation7DaysReminderConsumerTests
{
    private readonly ILogger<Confirmation7DaysReminderConsumer> _logger =
        Substitute.For<ILogger<Confirmation7DaysReminderConsumer>>();

    private readonly IEventProcessor<Confirmation7DaysReminderEvent> _processor =
        Substitute.For<IEventProcessor<Confirmation7DaysReminderEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<Confirmation7DaysReminderConsumer>(); })
            .AddSingleton(_processor)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new Confirmation7DaysReminderEvent
        {
            UserId = Guid.NewGuid(),
            Email = "unconfirmed@example.com",
            Name = "Kari Nordmann",
            ConfirmationLink = "https://kjokkenhylla.no/confirm?token=777",
            RegisteredAt = DateTime.UtcNow.AddDays(-7)
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<Confirmation7DaysReminderEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<Confirmation7DaysReminderEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
