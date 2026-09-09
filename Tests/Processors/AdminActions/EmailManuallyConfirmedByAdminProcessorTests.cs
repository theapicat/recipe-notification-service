using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.AdminActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Tests.Processors.AdminActions;

public class EmailManuallyConfirmedByAdminProcessorTests
{
    private readonly IOptions<AppSettings> _appSettings =
        Options.Create(new AppSettings { FrontendUrl = "https://kjokkenhylla.no" });

    private readonly ILogger<EmailManuallyConfirmedByAdminProcessor> _logger =
        Substitute.For<ILogger<EmailManuallyConfirmedByAdminProcessor>>();

    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();

    private readonly EmailManuallyConfirmedByAdminProcessor _processor;
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();

    public EmailManuallyConfirmedByAdminProcessorTests()
    {
        _processor = new EmailManuallyConfirmedByAdminProcessor(
            _templateRenderService,
            _pendingEmailService,
            _appSettings,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRenderTemplateAndProcessWithRetry()
    {
        // Arrange
        const string expectedHtml = "<html>E-post bekreftet av admin</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new EmailManuallyConfirmedByAdminEvent
        {
            UserId = Guid.NewGuid(),
            Email = "bekreftet@example.com",
            Name = "Kari Nordmann",
            ConfirmedAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("AdminActions/EmailManuallyConfirmedByAdmin", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "bekreftet@example.com",
                "E-postadressen din har blitt bekreftet av administrator",
                expectedHtml,
                nameof(EmailManuallyConfirmedByAdminEvent),
                Arg.Any<CancellationToken>());
    }
}