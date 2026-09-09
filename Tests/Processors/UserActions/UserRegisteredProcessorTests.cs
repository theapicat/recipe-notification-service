using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Exceptions;
using Infrastructure.Options;
using Infrastructure.Processors.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Tests.Processors.UserActions;

public class UserRegisteredProcessorTests
{
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();
    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();
    private readonly IOptions<AppSettings> _appSettings = Options.Create(new AppSettings { FrontendUrl = "https://kjokkenhylla.no" });
    private readonly ILogger<UserRegisteredProcessor> _logger = Substitute.For<ILogger<UserRegisteredProcessor>>();

    private readonly UserRegisteredProcessor _processor;

    public UserRegisteredProcessorTests()
    {
        _processor = new UserRegisteredProcessor(
            _templateRenderService,
            _pendingEmailService,
            _appSettings,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRenderWelcomeTemplate()
    {
        // Arrange
        const string expectedHtml = "<html>Velkommen til Kjøkkenhylla</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new UserRegisteredEvent
        {
            UserId = Guid.NewGuid(),
            Email = "nybruker@example.com",
            Name = "Kari Nordmann",
            ConfirmationLink = "https://kjokkenhylla.no/confirm?token=abc",
            RegisteredAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("UserActions/UserRegisteredWelcome", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "nybruker@example.com",
                "Velkommen til Kjøkkenhylla! Bekreft din e-postadresse",
                expectedHtml,
                nameof(UserRegisteredEvent),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenTemplateRenderFails_ShouldPropagateException()
    {
        // Arrange
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromException<string>(new TemplateRenderException("Feil ved rendring")));

        var @event = new UserRegisteredEvent
        {
            UserId = Guid.NewGuid(),
            Email = "nybruker@example.com",
            Name = "Kari Nordmann",
            ConfirmationLink = "https://kjokkenhylla.no/confirm?token=abc",
            RegisteredAt = DateTime.UtcNow
        };

        // Act & Assert
        await Should.ThrowAsync<TemplateRenderException>(() =>
            _processor.ProcessAsync(@event, CancellationToken.None));

        await _pendingEmailService.DidNotReceiveWithAnyArgs()
            .ProcessEmailWithRetryAsync(default!, default!, default!, default!, default);
    }
}