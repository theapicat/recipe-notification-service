using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.AdminActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Tests.Processors.AdminActions;

public class UserAccountDeletedByAdminProcessorTests
{
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();
    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();
    private readonly ILogger<UserAccountDeletedByAdminProcessor> _logger = Substitute.For<ILogger<UserAccountDeletedByAdminProcessor>>();

    private readonly UserAccountDeletedByAdminProcessor _processor;

    public UserAccountDeletedByAdminProcessorTests()
    {
        _processor = new UserAccountDeletedByAdminProcessor(
            _templateRenderService,
            _pendingEmailService,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRenderAccountDeletedTemplate()
    {
        // Arrange
        const string expectedHtml = "<html>Konto slettet av admin</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new UserAccountDeletedByAdminEvent
        {
            UserId = Guid.NewGuid(),
            Email = "slettet@example.com",
            Name = "Ola Nordmann",
            DeletedAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("AdminActions/UserAccountDeletedByAdmin", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "slettet@example.com",
                "Din brukerkonto hos Kjøkkenhylla har blitt slettet",
                expectedHtml,
                nameof(UserAccountDeletedByAdminEvent),
                Arg.Any<CancellationToken>());
    }
}