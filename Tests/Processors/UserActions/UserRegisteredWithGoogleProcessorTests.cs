using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Tests.Processors.UserActions;

public class UserRegisteredWithGoogleProcessorTests
{
    private readonly IOptions<AppSettings> _appSettings =
        Options.Create(new AppSettings { FrontendUrl = "https://kjokkenhylla.no" });

    private readonly ILogger<UserRegisteredWithGoogleProcessor> _logger =
        Substitute.For<ILogger<UserRegisteredWithGoogleProcessor>>();

    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();

    private readonly UserRegisteredWithGoogleProcessor _processor;
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();

    public UserRegisteredWithGoogleProcessorTests()
    {
        _processor = new UserRegisteredWithGoogleProcessor(
            _templateRenderService,
            _pendingEmailService,
            _appSettings,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRenderGoogleWelcomeTemplate()
    {
        // Arrange
        const string expectedHtml = "<html>Google Velkommen</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new UserRegisteredWithGoogleEvent
        {
            UserId = Guid.NewGuid(),
            Email = "google@example.com",
            Name = "Ola Nordmann",
            RegisteredAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("UserActions/UserRegisteredWithGoogleWelcome", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "google@example.com",
                "Velkommen til Kjøkkenhylla!",
                expectedHtml,
                nameof(UserRegisteredWithGoogleEvent),
                Arg.Any<CancellationToken>());
    }
}