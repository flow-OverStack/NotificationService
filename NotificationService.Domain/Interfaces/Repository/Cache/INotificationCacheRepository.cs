using NotificationService.Domain.Dtos.Notification;

namespace NotificationService.Domain.Interfaces.Repository.Cache;

public interface INotificationCacheRepository
{
    Task<IEnumerable<NotificationDto>?> GetAsync(long recipientId, bool unreadOnly, int? skip, int? take,
        CancellationToken cancellationToken = default);

    Task SetAsync(long recipientId, bool unreadOnly, int? skip, int? take,
        IEnumerable<NotificationDto> notifications, CancellationToken cancellationToken = default);

    Task InvalidateAsync(long recipientId, CancellationToken cancellationToken = default);
}
