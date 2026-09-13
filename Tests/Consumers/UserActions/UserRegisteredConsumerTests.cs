using Contracts.Events.UserActions;
using Infrastructure.Processors.Interfaces;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Service.Consumers.UserActions;
using Shouldly;

namespace Tests.Consumers.UserActions;

public class UserRegisteredConsumerTests
{
    private readonly ILogger<UserRegisteredConsumer> _logger = Substitute.For<ILogger<UserRegisteredConsumer>>();

    private readonly IEventProcessor<UserRegisteredEvent> _processor =
        Substitute.For<IEventProcessor<UserRegisteredEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<UserRegisteredConsumer>(); })
            .AddSingleton(_processor)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserRegisteredEvent
        {
            UserId = Guid.NewGuid(),
            Name = "Kari Nordmann",
            Email = "kari@example.com",
            ConfirmationLink = "https://kjokkenhylla.no/confirm?token=abc",
            RegisteredAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<UserRegisteredEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<UserRegisteredEvent>(e => e.Email == @event.Email), Arg.Any<CancellationToken>());
    }
}
