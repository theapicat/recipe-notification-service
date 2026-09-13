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

public class EmailManuallyConfirmedByAdminConsumerTests
{
    private readonly ILogger<EmailManuallyConfirmedByAdminConsumer> _logger =
        Substitute.For<ILogger<EmailManuallyConfirmedByAdminConsumer>>();

    private readonly IEventProcessor<EmailManuallyConfirmedByAdminEvent> _processor =
        Substitute.For<IEventProcessor<EmailManuallyConfirmedByAdminEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<EmailManuallyConfirmedByAdminConsumer>(); })
            .AddSingleton(_processor)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new EmailManuallyConfirmedByAdminEvent
        {
            UserId = Guid.NewGuid(),
            Email = "bekreftet@example.com",
            Name = "Kari Nordmann",
            ConfirmedAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<EmailManuallyConfirmedByAdminEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<EmailManuallyConfirmedByAdminEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
