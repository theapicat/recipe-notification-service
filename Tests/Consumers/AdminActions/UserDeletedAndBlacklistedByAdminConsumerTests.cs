using Contracts.Events.AdminActions;
using Infrastructure.Processors.Interfaces;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Service.Consumers.AdminActions;
using Shouldly;

namespace Tests.Consumers.AdminActions;

public class UserDeletedAndBlacklistedByAdminConsumerTests
{
    private readonly IEventProcessor<UserDeletedAndBlacklistedByAdminEvent> _processor =
        Substitute.For<IEventProcessor<UserDeletedAndBlacklistedByAdminEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<UserDeletedAndBlacklistedByAdminConsumer>(); })
            .AddSingleton(_processor)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserDeletedAndBlacklistedByAdminEvent
        {
            UserId = Guid.NewGuid(),
            Email = "blacklisted@example.com",
            Name = "Uønsket Bruker",
            Reason = "Brukervilkår brutt",
            DeletedAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<UserDeletedAndBlacklistedByAdminEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<UserDeletedAndBlacklistedByAdminEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
