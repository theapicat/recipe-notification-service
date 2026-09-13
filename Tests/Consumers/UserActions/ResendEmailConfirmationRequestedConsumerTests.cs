using Contracts.Events.UserActions;
using Infrastructure.Processors.Interfaces;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Service.Consumers.UserActions;
using Shouldly;

namespace Tests.Consumers.UserActions;

public class ResendEmailConfirmationRequestedConsumerTests
{
    private readonly IEventProcessor<ResendEmailConfirmationRequestedEvent> _processor =
        Substitute.For<IEventProcessor<ResendEmailConfirmationRequestedEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<ResendEmailConfirmationRequestedConsumer>(); })
            .AddSingleton(_processor)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new ResendEmailConfirmationRequestedEvent
        {
            UserId = Guid.NewGuid(),
            Email = "confirm@example.com",
            Name = "Ola Nordmann",
            ConfirmationLink = "https://kjokkenhylla.no/confirm?token=123",
            RequestedAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<ResendEmailConfirmationRequestedEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<ResendEmailConfirmationRequestedEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
