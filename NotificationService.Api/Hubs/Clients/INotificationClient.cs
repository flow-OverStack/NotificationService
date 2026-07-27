using NotificationService.Domain.Dtos.Notification;

namespace NotificationService.Api.Hubs.Clients;

public interface INotificationClient
{
    Task ReceiveNotification(NotificationDto notification);
}
