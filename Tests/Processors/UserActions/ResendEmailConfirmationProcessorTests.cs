using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Tests.Processors.UserActions;

public class ResendEmailConfirmationProcessorTests
{
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();
    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();
    private readonly IOptions<AppSettings> _appSettings = Options.Create(new AppSettings { FrontendUrl = "https://kjokkenhylla.no" });
    private readonly ILogger<ResendEmailConfirmationProcessor> _logger = Substitute.For<ILogger<ResendEmailConfirmationProcessor>>();

    private readonly ResendEmailConfirmationProcessor _processor;

    public ResendEmailConfirmationProcessorTests()
    {
        _processor = new ResendEmailConfirmationProcessor(
            _templateRenderService,
            _pendingEmailService,
            _appSettings,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRenderResendConfirmationTemplate()
    {
        // Arrange
        const string expectedHtml = "<html>Ny bekreftelseslenke</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new ResendEmailConfirmationRequestedEvent
        {
            UserId = Guid.NewGuid(),
            Email = "confirm@example.com",
            Name = "Ola Nordmann",
            ConfirmationLink = "https://kjokkenhylla.no/confirm?token=123",
            RequestedAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("UserActions/ResendEmailConfirmation", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "confirm@example.com",
                "Bekreft din e-postadresse - Kjøkkenhylla",
                expectedHtml,
                nameof(ResendEmailConfirmationRequestedEvent),
                Arg.Any<CancellationToken>());
    }
}