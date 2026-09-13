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

public class PasswordResetRequestedConsumerTests
{
    private readonly ILogger<PasswordResetRequestedConsumer> _logger =
        Substitute.For<ILogger<PasswordResetRequestedConsumer>>();

    private readonly IEventProcessor<PasswordResetRequestedEvent> _processor =
        Substitute.For<IEventProcessor<PasswordResetRequestedEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<PasswordResetRequestedConsumer>(); })
            .AddSingleton(_processor)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new PasswordResetRequestedEvent
        {
            UserId = Guid.NewGuid(),
            Email = "reset@example.com",
            Name = "Kari Nordmann",
            ResetLink = "https://kjokkenhylla.no/reset-password?token=xyz",
            RequestedAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<PasswordResetRequestedEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<PasswordResetRequestedEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
