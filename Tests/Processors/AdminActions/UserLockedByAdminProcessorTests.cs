using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.AdminActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Tests.Processors.AdminActions;

public class UserLockedByAdminProcessorTests
{
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();
    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();
    private readonly ILogger<UserLockedByAdminProcessor> _logger = Substitute.For<ILogger<UserLockedByAdminProcessor>>();

    private readonly UserLockedByAdminProcessor _processor;

    public UserLockedByAdminProcessorTests()
    {
        _processor = new UserLockedByAdminProcessor(
            _templateRenderService,
            _pendingEmailService,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRenderUserLockedTemplate()
    {
        // Arrange
        const string expectedHtml = "<html>Konto sperret</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new UserLockedByAdminEvent
        {
            UserId = Guid.NewGuid(),
            Email = "sperret@example.com",
            Name = "Ola Nordmann",
            ReasonDetails = "Misstanke om misbruk",
            LockedAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("AdminActions/UserLockedByAdmin", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "sperret@example.com",
                "Kontoen din hos Kjøkkenhylla har blitt sperret",
                expectedHtml,
                nameof(UserLockedByAdminEvent),
                Arg.Any<CancellationToken>());
    }
}