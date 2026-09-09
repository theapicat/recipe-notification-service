using Contracts.Events.SystemActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.SystemActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Tests.Processors.SystemActions;

public class Inactivity1YearLockedProcessorTests
{
    private readonly IOptions<AppSettings> _appSettings =
        Options.Create(new AppSettings { FrontendUrl = "https://kjokkenhylla.no" });

    private readonly ILogger<Inactivity1YearLockedProcessor> _logger =
        Substitute.For<ILogger<Inactivity1YearLockedProcessor>>();

    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();

    private readonly Inactivity1YearLockedProcessor _processor;
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();

    public Inactivity1YearLockedProcessorTests()
    {
        _processor = new Inactivity1YearLockedProcessor(
            _templateRenderService,
            _pendingEmailService,
            _appSettings,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRender1YearLockedTemplate()
    {
        // Arrange
        const string expectedHtml = "<html>Konto deaktivert 1 år</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new Inactivity1YearLockedEvent
        {
            UserId = Guid.NewGuid(),
            Email = "inactive1year@example.com",
            Name = "Ola Nordmann",
            LockedAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("SystemActions/Inactivity1YearLocked", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "inactive1year@example.com",
                "Kontoen din hos Kjøkkenhylla er midlertidig deaktivert pga. inaktivitet",
                expectedHtml,
                nameof(Inactivity1YearLockedEvent),
                Arg.Any<CancellationToken>());
    }
}