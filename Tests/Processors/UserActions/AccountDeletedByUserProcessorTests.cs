using Contracts.Events.UserActions;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.Exceptions;
using Infrastructure.Processors.UserActions;
using Infrastructure.TemplateService.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Tests.Processors.UserActions;

public class AccountDeletedByUserProcessorTests
{
    private readonly ILogger<AccountDeletedByUserProcessor> _logger =
        Substitute.For<ILogger<AccountDeletedByUserProcessor>>();

    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();

    private readonly AccountDeletedByUserProcessor _processor;
    private readonly ITemplateRenderService _templateRenderService = Substitute.For<ITemplateRenderService>();

    public AccountDeletedByUserProcessorTests()
    {
        _processor = new AccountDeletedByUserProcessor(
            _templateRenderService,
            _pendingEmailService,
            _logger);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidEvent_ShouldRenderTemplateAndProcessWithRetry()
    {
        // Arrange
        const string expectedHtml = "<html>Konto slettet av bruker</html>";
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromResult(expectedHtml));

        var @event = new UserAccountDeletedByUserEvent
        {
            UserId = Guid.NewGuid(),
            Email = "deleteduser@example.com",
            Name = "Ola Nordmann",
            DeletedAt = DateTime.UtcNow
        };

        // Act
        await _processor.ProcessAsync(@event, CancellationToken.None);

        // Assert
        await _templateRenderService.Received(1)
            .RenderTemplateAsync("UserActions/AccountDeletedByUser", Arg.Is<object>(x => x != null));

        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync(
                "deleteduser@example.com",
                "Bekreftelse på sletting av konto - Kjøkkenhylla",
                expectedHtml,
                nameof(UserAccountDeletedByUserEvent),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenTemplateRenderFails_ShouldPropagateException()
    {
        // Arrange
        _templateRenderService
            .RenderTemplateAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromException<string>(new TemplateRenderException("Mal-feil")));

        var @event = new UserAccountDeletedByUserEvent
        {
            UserId = Guid.NewGuid(),
            Email = "deleteduser@example.com",
            Name = "Ola Nordmann",
            DeletedAt = DateTime.UtcNow
        };

        // Act & Assert
        await Should.ThrowAsync<TemplateRenderException>(() =>
            _processor.ProcessAsync(@event, CancellationToken.None));

        await _pendingEmailService.DidNotReceiveWithAnyArgs()
            .ProcessEmailWithRetryAsync(default!, default!, default!, default!);
    }
}