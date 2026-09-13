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

public class AccountDeletedBySystemConsumerTests
{
    private readonly ILogger<AccountDeletedBySystemConsumer> _logger =
        Substitute.For<ILogger<AccountDeletedBySystemConsumer>>();

    private readonly IEventProcessor<UserAccountDeletedBySystemEvent> _processor =
        Substitute.For<IEventProcessor<UserAccountDeletedBySystemEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<AccountDeletedBySystemConsumer>(); })
            .AddSingleton(_processor)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserAccountDeletedBySystemEvent
        {
            UserId = Guid.NewGuid(),
            Email = "deleted@example.com",
            Name = "Ola Nordmann",
            DeletionReason = "Inaktivitet i over 30 dager",
            DeletedAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<UserAccountDeletedBySystemEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<UserAccountDeletedBySystemEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
