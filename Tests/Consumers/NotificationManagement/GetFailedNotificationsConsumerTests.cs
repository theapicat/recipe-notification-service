using Contracts.Queries.NotificationManagement;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Persistence.Entities;
using Persistence.Repositories.Interfaces;
using Service.Consumers.NotificationManagement;
using Shouldly;

namespace Tests.Consumers.NotificationManagement;

public class GetFailedNotificationsConsumerTests
{
    private readonly IFailedNotificationRepository _repository = Substitute.For<IFailedNotificationRepository>();

    [Fact]
    public async Task Consume_WhenGetFailedNotificationsQuerySent_ShouldReturnGetFailedNotificationsResponse()
    {
        // Arrange
        var mockFailedNotifications = new List<FailedNotification>
        {
            new()
            {
                Id = Guid.NewGuid(),
                RecipientEmail = "failed1@example.com",
                Subject = "Feilet 1",
                HtmlBody = "<html>Body 1</html>",
                EventType = "UserRegisteredEvent",
                LastErrorMessage = "SMTP Timeout",
                RetryCount = 5,
                CreatedAt = DateTime.UtcNow
            }
        };

        _repository.GetAllPendingAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockFailedNotifications));

        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.AddConsumer<GetFailedNotificationsConsumer>(); })
            .AddSingleton(_repository)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var client = harness.GetRequestClient<GetFailedNotificationsQuery>();

        // Act
        var response = await client.GetResponse<GetFailedNotificationsResponse>(new GetFailedNotificationsQuery());

        // Assert
        response.Message.ShouldNotBeNull();
        response.Message.Items.Count.ShouldBe(1);
        response.Message.Items.First().RecipientEmail.ShouldBe("failed1@example.com");
    }
}