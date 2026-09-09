using Contracts.Events.SystemActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.SystemActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Tests.Processors.SystemActions;

public class AccountDeletedBySystemProcessorTests
{
    private readonly IOptions<AppSettings> _appSettings =
        Options.Create(new AppSettings { FrontendUrl = "https://kjokkenhylla.no" });

    private readonly ILogger<AccountDeletedBySystemProcessor> _logger =
        Substitute.For<ILogger<AccountDeletedBySystemProcessor>>();

    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();

    private readonly AccountDeletedBySystemProcessor _processor;
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();

    public AccountDeletedBySystemProcessorTests()
    {
        _processor = new AccountDeletedBySystemProcessor(
            _templateRenderService,
            _pendingEmailService,
            _appSettings,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRenderTemplateAndProcessWithRetry()
    {
        // Arrange
        const string expectedHtml = "<html>Konto slettet av system</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new UserAccountDeletedBySystemEvent
        {
            UserId = Guid.NewGuid(),
            Email = "deleted@example.com",
            Name = "Ola Nordmann",
            DeletionReason = "Inaktivitet i over 30 dager",
            DeletedAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("SystemActions/AccountDeletedBySystem", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "deleted@example.com",
                "Din konto hos Kjøkkenhylla har blitt slettet",
                expectedHtml,
                nameof(UserAccountDeletedBySystemEvent),
                Arg.Any<CancellationToken>());
    }
}