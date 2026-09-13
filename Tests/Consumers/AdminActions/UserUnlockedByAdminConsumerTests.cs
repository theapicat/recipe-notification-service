using Contracts.Events.AdminActions;
using Infrastructure.Processors.Interfaces;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Service.Consumers.AdminActions;
using Shouldly;

namespace Tests.Consumers.AdminActions;

public class UserUnlockedByAdminConsumerTests
{
    private readonly ILogger<UserUnlockedByAdminConsumer> _logger =
        Substitute.For<ILogger<UserUnlockedByAdminConsumer>>();

    private readonly IEventProcessor<UserUnlockedByAdminEvent> _processor =
        Substitute.For<IEventProcessor<UserUnlockedByAdminEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<UserUnlockedByAdminConsumer>(); })
            .AddSingleton(_processor)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserUnlockedByAdminEvent
        {
            UserId = Guid.NewGuid(),
            Email = "gjenapnet@example.com",
            Name = "Kari Nordmann",
            UnlockedAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<UserUnlockedByAdminEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<UserUnlockedByAdminEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
