using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Options;
using Infrastructure.Processors.AdminActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Tests.Processors.AdminActions;

public class UserUnlockedByAdminProcessorTests
{
    private readonly IOptions<AppSettings> _appSettings =
        Options.Create(new AppSettings { FrontendUrl = "https://kjokkenhylla.no" });

    private readonly ILogger<UserUnlockedByAdminProcessor> _logger =
        Substitute.For<ILogger<UserUnlockedByAdminProcessor>>();

    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();

    private readonly UserUnlockedByAdminProcessor _processor;
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();

    public UserUnlockedByAdminProcessorTests()
    {
        _processor = new UserUnlockedByAdminProcessor(
            _templateRenderService,
            _pendingEmailService,
            _appSettings,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRenderUserUnlockedTemplate()
    {
        // Arrange
        const string expectedHtml = "<html>Konto gjenåpnet</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new UserUnlockedByAdminEvent
        {
            UserId = Guid.NewGuid(),
            Email = "gjenapnet@example.com",
            Name = "Kari Nordmann",
            UnlockedAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("AdminActions/UserUnlockedByAdmin", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "gjenapnet@example.com",
                "Kontoen din hos Kjøkkenhylla har blitt gjenåpnet",
                expectedHtml,
                nameof(UserUnlockedByAdminEvent),
                Arg.Any<CancellationToken>());
    }
}