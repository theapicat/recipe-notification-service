using Contracts.Queries.NotificationManagement;
using MassTransit;
using Persistence.Repositories.Interfaces;

namespace Service.Consumers.NotificationManagement;

public class GetFailedNotificationsConsumer(IFailedNotificationRepository repository) 
    : IConsumer<GetFailedNotificationsQuery>
{
    public async Task Consume(ConsumeContext<GetFailedNotificationsQuery> context)
    {
        var failedNotifications = await repository.GetAllPendingAsync(context.CancellationToken);

        var response = new GetFailedNotificationsResponse
        {
            Items = failedNotifications.Select(x => new FailedNotificationDto
            {
                Id = x.Id,
                RecipientEmail = x.RecipientEmail,
                Subject = x.Subject,
                HtmlBody = x.HtmlBody,
                EventType = x.EventType,
                LastErrorMessage = x.LastErrorMessage,
                RetryCount = x.RetryCount,
                CreatedAt = x.CreatedAt,
                LastAttemptAt = x.LastAttemptAt
            }).ToList()
        };

        await context.RespondAsync(response);
    }
}