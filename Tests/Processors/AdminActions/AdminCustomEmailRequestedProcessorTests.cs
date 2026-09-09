using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Exceptions;
using Infrastructure.Processors.AdminActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Tests.Processors.AdminActions;

public class AdminCustomEmailRequestedProcessorTests
{
    private readonly ILogger<AdminCustomEmailRequestedProcessor> _logger =
        Substitute.For<ILogger<AdminCustomEmailRequestedProcessor>>();

    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();

    private readonly AdminCustomEmailRequestedProcessor _processor;
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();

    public AdminCustomEmailRequestedProcessorTests()
    {
        _processor = new AdminCustomEmailRequestedProcessor(
            _templateRenderService,
            _pendingEmailService,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRenderTemplateAndProcessWithRetry()
    {
        // Arrange
        const string expectedHtml = "<html>Melding fra admin</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new AdminCustomEmailRequestedEvent
        {
            UserId = Guid.NewGuid(),
            Email = "bruker@example.com",
            Name = "Ola Nordmann",
            Subject = "Viktig melding angående konto",
            Message = "Dette er en spesialmelding.",
            SentAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("AdminActions/AdminCustomEmail", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "bruker@example.com",
                "Viktig melding angående konto",
                expectedHtml,
                nameof(AdminCustomEmailRequestedEvent),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenTemplateRenderFails_ShouldPropagateException()
    {
        // Arrange
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromException<string>(new TemplateRenderException("Mal-feil")));

        var @event = new AdminCustomEmailRequestedEvent
        {
            UserId = Guid.NewGuid(),
            Email = "bruker@example.com",
            Name = "Ola",
            Subject = "Test",
            Message = "Test",
            SentAt = DateTime.UtcNow
        };

        // Act & Assert
        await Should.ThrowAsync<TemplateRenderException>(() =>
            _processor.ProcessAsync(@event, CancellationToken.None));

        await _pendingEmailService.DidNotReceiveWithAnyArgs()
            .ProcessEmailWithRetryAsync(default!, default!, default!, default!);
    }
}