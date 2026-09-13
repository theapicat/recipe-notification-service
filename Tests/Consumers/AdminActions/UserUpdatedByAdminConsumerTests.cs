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

public class UserUpdatedByAdminConsumerTests
{
    private readonly ILogger<UserUpdatedByAdminConsumer> _logger =
        Substitute.For<ILogger<UserUpdatedByAdminConsumer>>();

    private readonly IEventProcessor<UserUpdatedByAdminEvent> _processor =
        Substitute.For<IEventProcessor<UserUpdatedByAdminEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<UserUpdatedByAdminConsumer>(); })
            .AddSingleton(_processor)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserUpdatedByAdminEvent
        {
            UserId = Guid.NewGuid(),
            Email = "oppdatert@example.com",
            Name = "Ola Nordmann",
            OldEmail = "gammel@example.com",
            NewEmail = "oppdatert@example.com",
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<UserUpdatedByAdminEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<UserUpdatedByAdminEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
