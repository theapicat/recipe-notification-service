using Contracts.Commands.NotificationManagement;
using Infrastructure.EmailDelivery.Interfaces;
using Infrastructure.State.Interfaces;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Persistence.Entities;
using Persistence.Repositories.Interfaces;
using Service.Consumers.NotificationManagement;
using Shouldly;

namespace Tests.Consumers.NotificationManagement;

public class RetryFailedNotificationCommandConsumerTests
{
    private readonly ILogger<RetryFailedNotificationCommandConsumer> _logger =
        Substitute.For<ILogger<RetryFailedNotificationCommandConsumer>>();

    private readonly IPendingEmailService _pendingEmailService = Substitute.For<IPendingEmailService>();
    private readonly IFailedNotificationRepository _repository = Substitute.For<IFailedNotificationRepository>();
    private readonly INotificationStateStore _stateStore = Substitute.For<INotificationStateStore>();

    [Fact]
    public async Task Consume_WhenRetryCommandReceived_ShouldResetRetryCountProcessEmailAndRemoveFromDb()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var idsToRetry = new List<Guid> { targetId };

        var pendingEmails = new List<FailedNotification>
        {
            new()
            {
                Id = targetId,
                RecipientEmail = "retry@example.com",
                Subject = "Emne",
                HtmlBody = "<html>Body</html>",
                EventType = "UserRegisteredEvent"
            }
        };

        _repository.GetAllPendingAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pendingEmails));

        _repository.GetPendingCountAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0L)); // Bufferen er tom etter at denne er slettet

        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<RetryFailedNotificationCommandConsumer>(); })
            .AddSingleton(_repository)
            .AddSingleton(_pendingEmailService)
            .AddSingleton(_stateStore)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var command = new RetryFailedNotificationCommand { NotificationIds = idsToRetry };

        // Act
        await harness.Bus.Publish(command);

        // Assert
        (await harness.Consumed.Any<RetryFailedNotificationCommand>()).ShouldBeTrue();

        // 1. Tilbakestiller teller i repo
        await _repository.Received(1)
            .ResetRetryCountManyAsync(Arg.Is<List<Guid>>(x => x.Contains(targetId)), Arg.Any<CancellationToken>());

        // 2. Sender e-post på nytt via PendingEmailService
        await _pendingEmailService.Received(1)
            .ProcessEmailWithRetryAsync("retry@example.com", "Emne", "<html>Body</html>", "UserRegisteredEvent",
                Arg.Any<CancellationToken>());

        // 3. Sletter fra MongoDB
        await _repository.Received(1)
            .DeleteAsync(targetId, Arg.Any<CancellationToken>());

        // 4. Tilbakestiller state store siden teller er 0
        _stateStore.Received(1)
            .Reset();
    }
}