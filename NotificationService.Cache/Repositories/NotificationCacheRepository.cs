using Microsoft.Extensions.Options;
using NotificationService.Cache.Helpers;
using NotificationService.Cache.Settings;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Interface.Provider;
using NotificationService.Domain.Interface.Repository.Cache;

namespace NotificationService.Cache.Repositories;

public class NotificationCacheRepository(ICacheProvider cache, IOptions<RedisSettings> redisSettings)
    : INotificationCacheRepository
{
    private readonly int _timeToLiveInSeconds = redisSettings.Value.TimeToLiveInSeconds;

    public async Task<IEnumerable<NotificationDto>?> GetAsync(long recipientId, bool unreadOnly, int? skip,
        int? take, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = CacheKeyHelper.GetRecipientNotificationsKey(recipientId, unreadOnly, skip, take);
            return await cache.GetJsonParsedAsync<NotificationDto[]>(key, cancellationToken);
        }
        catch (Exception)
        {
            // If reading from the cache fails, we treat it as a miss.
            return null;
        }
    }

    public async Task SetAsync(long recipientId, bool unreadOnly, int? skip, int? take,
        IEnumerable<NotificationDto> notifications, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = CacheKeyHelper.GetRecipientNotificationsKey(recipientId, unreadOnly, skip, take);
            var indexKey = CacheKeyHelper.GetRecipientNotificationKeysKey(recipientId);

            await cache.StringSetAsync(key, notifications.ToArray(), _timeToLiveInSeconds, true,
                CancellationToken.None);
            await cache.SetsAddAsync(indexKey, [key], _timeToLiveInSeconds, true, CancellationToken.None);
        }
        catch (Exception)
        {
            // If caching fails, we still return the fetched data without caching it.
        }
    }

    public async Task InvalidateAsync(long recipientId, CancellationToken cancellationToken = default)
    {
        try
        {
            var indexKey = CacheKeyHelper.GetRecipientNotificationKeysKey(recipientId);
            var pageKeys = (await cache.SetStringMembersAsync(indexKey, CancellationToken.None)).ToArray();

            await cache.KeysDeleteAsync([.. pageKeys, indexKey], true, CancellationToken.None);
        }
        catch (Exception)
        {
            // If invalidation fails, the entry stays stale until it expires via TTL.
        }
    }
}