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

public class AccountDeletedByUserConsumerTests
{
    private readonly ILogger<AccountDeletedByUserConsumer> _logger =
        Substitute.For<ILogger<AccountDeletedByUserConsumer>>();

    private readonly IEventProcessor<UserAccountDeletedByUserEvent> _processor =
        Substitute.For<IEventProcessor<UserAccountDeletedByUserEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<AccountDeletedByUserConsumer>(); })
            .AddSingleton(_processor)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserAccountDeletedByUserEvent
        {
            UserId = Guid.NewGuid(),
            Email = "deleteduser@example.com",
            Name = "Ola Nordmann",
            DeletedAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<UserAccountDeletedByUserEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<UserAccountDeletedByUserEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
