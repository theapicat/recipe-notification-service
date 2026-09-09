using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Tests.Processors.UserActions;

public class PasswordChangedProcessorTests
{
    private readonly ILogger<PasswordChangedProcessor> _logger = Substitute.For<ILogger<PasswordChangedProcessor>>();
    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();

    private readonly PasswordChangedProcessor _processor;
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();

    public PasswordChangedProcessorTests()
    {
        _processor = new PasswordChangedProcessor(
            _templateRenderService,
            _pendingEmailService,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRenderSecurityNoticeTemplate()
    {
        // Arrange
        const string expectedHtml = "<html>Sikkerhetsvarsel: Passord endret</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new PasswordChangedEvent
        {
            UserId = Guid.NewGuid(),
            Email = "user@example.com",
            Name = "Ola Nordmann",
            ChangedAt = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            DeviceInfo = "Chrome på Linux"
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("UserActions/PasswordChangedSecurityNotice", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "user@example.com",
                "Sikkerhetsvarsel: Passordet ditt har blitt endret",
                expectedHtml,
                nameof(PasswordChangedEvent),
                Arg.Any<CancellationToken>());
    }
}