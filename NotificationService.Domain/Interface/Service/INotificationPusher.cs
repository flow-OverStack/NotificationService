using NotificationService.Domain.Dtos.Notification;

namespace NotificationService.Domain.Interface.Service;

public interface INotificationPusher
{
    Task PushAsync(long recipientId, NotificationDto dto, CancellationToken cancellationToken = default);
}