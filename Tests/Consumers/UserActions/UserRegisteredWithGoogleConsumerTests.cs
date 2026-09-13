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

public class UserRegisteredWithGoogleConsumerTests
{
    private readonly ILogger<UserRegisteredWithGoogleConsumer> _logger =
        Substitute.For<ILogger<UserRegisteredWithGoogleConsumer>>();

    private readonly IEventProcessor<UserRegisteredWithGoogleEvent> _processor =
        Substitute.For<IEventProcessor<UserRegisteredWithGoogleEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<UserRegisteredWithGoogleConsumer>(); })
            .AddSingleton(_processor)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserRegisteredWithGoogleEvent
        {
            UserId = Guid.NewGuid(),
            Email = "google@example.com",
            Name = "Ola Nordmann",
            RegisteredAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<UserRegisteredWithGoogleEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<UserRegisteredWithGoogleEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
