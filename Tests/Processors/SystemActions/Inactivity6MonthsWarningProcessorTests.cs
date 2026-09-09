using Contracts.Events.SystemActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.SystemActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Tests.Processors.SystemActions;

public class Inactivity6MonthsWarningProcessorTests
{
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();
    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();
    private readonly IOptions<AppSettings> _appSettings = Options.Create(new AppSettings { FrontendUrl = "https://kjokkenhylla.no" });
    private readonly ILogger<Inactivity6MonthsWarningProcessor> _logger = Substitute.For<ILogger<Inactivity6MonthsWarningProcessor>>();

    private readonly Inactivity6MonthsWarningProcessor _processor;

    public Inactivity6MonthsWarningProcessorTests()
    {
        _processor = new Inactivity6MonthsWarningProcessor(
            _templateRenderService,
            _pendingEmailService,
            _appSettings,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRender6MonthsWarningTemplate()
    {
        // Arrange
        const string expectedHtml = "<html>Vi savner deg</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new Inactivity6MonthsWarningEvent
        {
            UserId = Guid.NewGuid(),
            Email = "inactive@example.com",
            Name = "Kari Nordmann",
            WarnedAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("SystemActions/Inactivity6MonthsWarning", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "inactive@example.com",
                "Vi savner deg på Kjøkkenhylla!",
                expectedHtml,
                nameof(Inactivity6MonthsWarningEvent),
                Arg.Any<CancellationToken>());
    }
}