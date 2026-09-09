using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Processors.AdminActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Tests.Processors.AdminActions;

public class UserUpdatedByAdminProcessorTests
{
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();
    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();
    private readonly ILogger<UserUpdatedByAdminProcessor> _logger = Substitute.For<ILogger<UserUpdatedByAdminProcessor>>();

    private readonly UserUpdatedByAdminProcessor _processor;

    public UserUpdatedByAdminProcessorTests()
    {
        _processor = new UserUpdatedByAdminProcessor(
            _templateRenderService,
            _pendingEmailService,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRenderUserUpdatedTemplate()
    {
        // Arrange
        const string expectedHtml = "<html>Profil oppdatert</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new UserUpdatedByAdminEvent
        {
            UserId = Guid.NewGuid(),
            Email = "oppdatert@example.com",
            Name = "Ola Nordmann",
            OldEmail = "gammel@example.com",
            NewEmail = "oppdatert@example.com",
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("AdminActions/UserUpdatedByAdmin", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "oppdatert@example.com",
                "Profilinformasjonen din hos Kjøkkenhylla har blitt oppdatert",
                expectedHtml,
                nameof(UserUpdatedByAdminEvent),
                Arg.Any<CancellationToken>());
    }
}