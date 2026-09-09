using Contracts.Events.SystemActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.SystemActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Tests.Processors.SystemActions;

public class Confirmation7DaysReminderProcessorTests
{
    private readonly IOptions<AppSettings> _appSettings =
        Options.Create(new AppSettings { FrontendUrl = "https://kjokkenhylla.no" });

    private readonly ILogger<Confirmation7DaysReminderProcessor> _logger =
        Substitute.For<ILogger<Confirmation7DaysReminderProcessor>>();

    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();

    private readonly Confirmation7DaysReminderProcessor _processor;
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();

    public Confirmation7DaysReminderProcessorTests()
    {
        _processor = new Confirmation7DaysReminderProcessor(
            _templateRenderService,
            _pendingEmailService,
            _appSettings,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRender7DaysReminderTemplate()
    {
        // Arrange
        const string expectedHtml = "<html>Påminnelse 7 dager</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new Confirmation7DaysReminderEvent
        {
            UserId = Guid.NewGuid(),
            Email = "unconfirmed@example.com",
            Name = "Kari Nordmann",
            ConfirmationLink = "https://kjokkenhylla.no/confirm?token=777",
            RegisteredAt = DateTime.UtcNow.AddDays(-7)
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("SystemActions/Confirmation7DaysReminder", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "unconfirmed@example.com",
                "Påminnelse: Bekreft din e-postadresse - Kjøkkenhylla",
                expectedHtml,
                nameof(Confirmation7DaysReminderEvent),
                Arg.Any<CancellationToken>());
    }
}