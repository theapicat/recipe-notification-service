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

public class Confirmation14DaysReminderProcessorTests
{
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();
    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();
    private readonly IOptions<AppSettings> _appSettings = Options.Create(new AppSettings { FrontendUrl = "https://kjokkenhylla.no" });
    private readonly ILogger<Confirmation14DaysReminderProcessor> _logger = Substitute.For<ILogger<Confirmation14DaysReminderProcessor>>();

    private readonly Confirmation14DaysReminderProcessor _processor;

    public Confirmation14DaysReminderProcessorTests()
    {
        _processor = new Confirmation14DaysReminderProcessor(
            _templateRenderService,
            _pendingEmailService,
            _appSettings,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRender14DaysReminderTemplate()
    {
        // Arrange
        const string expectedHtml = "<html>Sperret 14 dager</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new Confirmation14DaysReminderEvent
        {
            UserId = Guid.NewGuid(),
            Email = "locked@example.com",
            Name = "Ola Nordmann",
            ConfirmationLink = "https://kjokkenhylla.no/confirm?token=1414",
            LockedAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("SystemActions/Confirmation14DaysReminder", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "locked@example.com",
                "Kontoen din er midlertidig sperret - Kjøkkenhylla",
                expectedHtml,
                nameof(Confirmation14DaysReminderEvent),
                Arg.Any<CancellationToken>());
    }
}