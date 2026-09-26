using Contracts.Events.AdminActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Exceptions;
using Infrastructure.Processors.AdminActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Tests.Processors.AdminActions;

public class UserDeletedAndBlacklistedByAdminProcessorTests
{
    private readonly ILogger<UserDeletedAndBlacklistedByAdminProcessor> _logger =
        Substitute.For<ILogger<UserDeletedAndBlacklistedByAdminProcessor>>();

    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();

    private readonly UserDeletedAndBlacklistedByAdminProcessor _processor;
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();

    public UserDeletedAndBlacklistedByAdminProcessorTests()
    {
        _processor = new UserDeletedAndBlacklistedByAdminProcessor(
            _templateRenderService,
            _pendingEmailService,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRenderBlacklistedTemplate()
    {
        // Arrange
        const string expectedHtml = "<html>Konto utestengt</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new UserDeletedAndBlacklistedByAdminEvent
        {
            UserId = Guid.NewGuid(),
            Email = "blacklisted@example.com",
            Name = "Uønsket Bruker",
            Reason = "Brukervilkår brutt",
            DeletedAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("AdminActions/UserDeletedAndBlacklistedByAdmin", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "blacklisted@example.com",
                "Din brukerkonto hos Kjøkkenhylla har blitt slettet og utestengt",
                expectedHtml,
                nameof(UserDeletedAndBlacklistedByAdminEvent),
                Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ProcessAsync_WhenReasonIsMissing_ShouldUseDefaultReasonText(string reason)
    {
        // Arrange
        object capturedModel = null;
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Do<object>(m => capturedModel = m))
            .Returns(Task.FromResult("<html></html>"));

        var @event = new UserDeletedAndBlacklistedByAdminEvent
        {
            UserId = Guid.NewGuid(),
            Email = "blacklisted@example.com",
            Name = "Uønsket Bruker",
            Reason = reason,
            DeletedAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        capturedModel.ShouldNotBeNull();
        capturedModel.GetType().GetProperty("reason")!.GetValue(capturedModel)
            .ShouldBe(UserDeletedAndBlacklistedByAdminProcessor.MissingReasonText);
    }

    [Fact]
    public async Task ProcessAsync_WhenTemplateRenderFails_ShouldPropagateException()
    {
        // Arrange
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromException<string>(new TemplateRenderException("Mal-feil")));

        var @event = new UserDeletedAndBlacklistedByAdminEvent
        {
            UserId = Guid.NewGuid(),
            Email = "blacklisted@example.com",
            Name = "Uønsket Bruker",
            Reason = "Brukervilkår brutt",
            DeletedAt = DateTime.UtcNow
        };

        // Act & Assert
        await Should.ThrowAsync<TemplateRenderException>(() =>
            _processor.ProcessAsync(@event, CancellationToken.None));

        await _pendingEmailService.DidNotReceiveWithAnyArgs()
            .ProcessEmailWithRetryAsync(default!, default!, default!, default!);
    }
}