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

public class UserAccountDeletedByAdminConsumerTests
{
    private readonly ILogger<UserAccountDeletedByAdminConsumer> _logger =
        Substitute.For<ILogger<UserAccountDeletedByAdminConsumer>>();

    private readonly IEventProcessor<UserAccountDeletedByAdminEvent> _processor =
        Substitute.For<IEventProcessor<UserAccountDeletedByAdminEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<UserAccountDeletedByAdminConsumer>(); })
            .AddSingleton(_processor)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserAccountDeletedByAdminEvent
        {
            UserId = Guid.NewGuid(),
            Email = "slettet@example.com",
            Name = "Ola Nordmann",
            DeletedAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<UserAccountDeletedByAdminEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<UserAccountDeletedByAdminEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
