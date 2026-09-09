using Contracts.Commands.NotificationManagement;
using Infrastructure.State.Interfaces;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Persistence.Repositories.Interfaces;
using Service.Consumers.NotificationManagement;
using Shouldly;
using Xunit;

namespace Tests.Consumers.NotificationManagement;

public class DeleteFailedNotificationCommandConsumerTests
{
    private readonly IFailedNotificationRepository _repository = Substitute.For<IFailedNotificationRepository>();
    private readonly INotificationStateStore _stateStore = Substitute.For<INotificationStateStore>();
    private readonly ILogger<DeleteFailedNotificationCommandConsumer> _logger = Substitute.For<ILogger<DeleteFailedNotificationCommandConsumer>>();

    [Fact]
    public async Task Consume_WhenDeleteCommandWithIdsReceived_ShouldDeleteManyAndResetStateStoreIfEmpty()
    {
        // Arrange
        var idsToDelete = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        _repository.GetPendingCountAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0L)); // Ingen igjenværende feil i MongoDB

        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<DeleteFailedNotificationCommandConsumer>();
            })
            .AddSingleton(_repository)
            .AddSingleton(_stateStore)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var command = new DeleteFailedNotificationCommand { NotificationIds = idsToDelete };

        // Act
        await harness.Bus.Publish(command);

        // Assert
        (await harness.Consumed.Any<DeleteFailedNotificationCommand>()).ShouldBeTrue();

        await _repository.Received(1)
            .DeleteManyAsync(Arg.Is<List<Guid>>(x => x.Count == 2), Arg.Any<CancellationToken>());

        _stateStore.Received(1)
            .Reset();
    }

    [Fact]
    public async Task Consume_WhenEmptyListProvided_ShouldNotInvokeRepository()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<DeleteFailedNotificationCommandConsumer>();
            })
            .AddSingleton(_repository)
            .AddSingleton(_stateStore)
            .AddSingleton(_logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var command = new DeleteFailedNotificationCommand { NotificationIds = [] };

        // Act
        await harness.Bus.Publish(command);

        // Assert
        (await harness.Consumed.Any<DeleteFailedNotificationCommand>()).ShouldBeTrue();

        await _repository.DidNotReceiveWithAnyArgs()
            .DeleteManyAsync(default!, default);
    }
}