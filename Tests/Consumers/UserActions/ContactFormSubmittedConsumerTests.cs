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

public class ContactFormSubmittedConsumerTests
{
    private readonly ILogger<ContactFormSubmittedConsumer> _logger =
        Substitute.For<ILogger<ContactFormSubmittedConsumer>>();

    private readonly IEventProcessor<ContactFormSubmittedEvent> _processor =
        Substitute.For<IEventProcessor<ContactFormSubmittedEvent>>();

    [Fact]
    public async Task Consume_WhenEventReceived_ShouldInvokeProcessor()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<ContactFormSubmittedConsumer>(); })
            .AddSingleton(_processor)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new ContactFormSubmittedEvent
        {
            Name = "Kari Nordmann",
            Email = "kari@example.com",
            Subject = "Spørsmål angående oppskrifter",
            Message = "Hvordan legger jeg til favoritter?",
            SubmittedAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(@event);

        // Assert
        (await harness.Consumed.Any<ContactFormSubmittedEvent>()).ShouldBeTrue();

        await _processor.Received(1)
            .ProcessAsync(Arg.Is<ContactFormSubmittedEvent>(e => e.Email == @event.Email),
                Arg.Any<CancellationToken>());
    }
}
