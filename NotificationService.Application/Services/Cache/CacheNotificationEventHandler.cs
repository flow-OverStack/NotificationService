using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Dtos.UserEvent;
using NotificationService.Domain.Interfaces.Repository.Cache;
using NotificationService.Domain.Interfaces.Service;
using NotificationService.Domain.Results;

namespace NotificationService.Application.Services.Cache;

public class CacheNotificationEventHandler(
    INotificationCacheRepository cacheRepository,
    INotificationEventHandler inner) : INotificationEventHandler
{
    public async Task<BaseResult<NotificationDto>> CreateAsync(UserEventDto eventDto,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.CreateAsync(eventDto, cancellationToken);

        if (result.IsSuccess)
            await cacheRepository.InvalidateAsync(eventDto.RecipientId, CancellationToken.None);

        return result;
    }
}
