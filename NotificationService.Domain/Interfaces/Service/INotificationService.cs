using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Domain.Dtos.UserEvent;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Results;

namespace NotificationService.Domain.Interfaces.Service;

public interface INotificationService
{
    Task<BaseResult<NotificationDto>> MarkAsReadAsync(long id, long userId, CancellationToken cancellationToken = default);

    Task<CollectionResult<NotificationDto>> GetAllByRecipientIdAsync(long recipientId, bool unreadOnly,
        PaginationParams paginationParams, CancellationToken cancellationToken = default);
}