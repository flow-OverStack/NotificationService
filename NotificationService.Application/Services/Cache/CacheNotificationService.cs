using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Dtos.Pagination;
using NotificationService.Domain.Interface.Repository.Cache;
using NotificationService.Domain.Interface.Service;
using NotificationService.Domain.Results;

namespace NotificationService.Application.Services.Cache;

public class CacheNotificationService(
    INotificationCacheRepository cacheRepository,
    INotificationService inner) : INotificationService
{
    public async Task<BaseResult<NotificationDto>> MarkAsReadAsync(long id, long userId,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.MarkAsReadAsync(id, userId, cancellationToken);

        if (result.IsSuccess)
            await cacheRepository.InvalidateAsync(userId, CancellationToken.None);

        return result;
    }

    public async Task<CollectionResult<NotificationDto>> GetAllByRecipientIdAsync(long recipientId, bool unreadOnly,
        PaginationParams paginationParams, CancellationToken cancellationToken = default)
    {
        var cached = await cacheRepository.GetAsync(recipientId, unreadOnly, paginationParams.Skip,
            paginationParams.Take, cancellationToken);

        if (cached != null)
            return CollectionResult<NotificationDto>.Success(cached);

        var result = await inner.GetAllByRecipientIdAsync(recipientId, unreadOnly, paginationParams,
            cancellationToken);

        if (result.IsSuccess)
            await cacheRepository.SetAsync(recipientId, unreadOnly, paginationParams.Skip, paginationParams.Take,
                result.Data, CancellationToken.None);

        return result;
    }
}
