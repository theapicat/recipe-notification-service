using Contracts.Events.AdminActions;
using Infrastructure.Processors.Interfaces.AdminActions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Service.Consumers.AdminActions;
using Shouldly;

namespace Tests.Consumers.AdminActions;

public class AdminCustomEmailRequestedConsumerTests
{
    private readonly ILogger<AdminCustomEmailRequestedConsumer> _logger =
        Substitute.For<ILogger<AdminCustomEmailRequestedConsumer>>();

    private readonly IAdminCustomEmailRequestedProcessor _processor =
        Substitute.For<IAdminCustomEmailRequestedProcessor>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<AdminCustomEmailRequestedConsumer>(); })
            .AddSingleton(_processor)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new AdminCustomEmailRequestedEvent
        {
            UserId = Guid.NewGuid(),
            Email = "admin-send@example.com",
            Name = "Mottaker",
            Subject = "Emne",
            Message = "Melding",
            SentAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<AdminCustomEmailRequestedEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<AdminCustomEmailRequestedEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}