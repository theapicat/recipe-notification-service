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

public class UserLockedByAdminConsumerTests
{
    private readonly ILogger<UserLockedByAdminConsumer> _logger =
        Substitute.For<ILogger<UserLockedByAdminConsumer>>();

    private readonly IEventProcessor<UserLockedByAdminEvent> _processor =
        Substitute.For<IEventProcessor<UserLockedByAdminEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<UserLockedByAdminConsumer>(); })
            .AddSingleton(_processor)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserLockedByAdminEvent
        {
            UserId = Guid.NewGuid(),
            Email = "sperret@example.com",
            Name = "Ola Nordmann",
            ReasonDetails = "Mistanke om misbruk",
            LockedAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<UserLockedByAdminEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<UserLockedByAdminEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
