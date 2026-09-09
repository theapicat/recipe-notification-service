using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Tests.Processors.UserActions;

public class PasswordResetRequestedProcessorTests
{
    private readonly ILogger<PasswordResetRequestedProcessor> _logger =
        Substitute.For<ILogger<PasswordResetRequestedProcessor>>();

    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();

    private readonly PasswordResetRequestedProcessor _processor;
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();

    public PasswordResetRequestedProcessorTests()
    {
        _processor = new PasswordResetRequestedProcessor(
            _templateRenderService,
            _pendingEmailService,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRenderPasswordResetTemplate()
    {
        // Arrange
        const string expectedHtml = "<html>Tilbakestillingslenke</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new PasswordResetRequestedEvent
        {
            UserId = Guid.NewGuid(),
            Email = "reset@example.com",
            Name = "Kari Nordmann",
            ResetLink = "https://kjokkenhylla.no/reset-password?token=xyz",
            RequestedAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("UserActions/PasswordResetRequested", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "reset@example.com",
                "Tilbakestill ditt passord på Kjøkkenhylla",
                expectedHtml,
                nameof(PasswordResetRequestedEvent),
                Arg.Any<CancellationToken>());
    }
}