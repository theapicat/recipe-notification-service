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

public class PasswordChangedConsumerTests
{
    private readonly ILogger<PasswordChangedConsumer> _logger = Substitute.For<ILogger<PasswordChangedConsumer>>();

    private readonly IEventProcessor<PasswordChangedEvent> _processor =
        Substitute.For<IEventProcessor<PasswordChangedEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<PasswordChangedConsumer>(); })
            .AddSingleton(_processor)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new PasswordChangedEvent
        {
            UserId = Guid.NewGuid(),
            Name = "Ola Nordmann",
            Email = "user@example.com",
            ChangedAt = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            DeviceInfo = "Chrome på Linux"
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<PasswordChangedEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<PasswordChangedEvent>(e => e.Email == @event.Email), Arg.Any<CancellationToken>());
    }
}
