using Microsoft.Extensions.Options;
using NotificationService.Cache.Helpers;
using NotificationService.Cache.Settings;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Interfaces.Provider;
using NotificationService.Domain.Interfaces.Repository.Cache;
using Serilog;
using StackExchange.Redis;

namespace NotificationService.Cache.Repositories;

public class NotificationCacheRepository(ICacheProvider cache, IOptions<RedisSettings> redisSettings, ILogger logger)
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
        catch (RedisException e)
        {
            // If reading from the cache fails, we treat it as a miss.
            logger.Warning(e, "Cache read failed, falling back to source");
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
        catch (RedisException e)
        {
            // If caching fails, we still return the fetched data without caching it.
            logger.Warning(e, "Cache write failed, could not cache fetched data");
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
        catch (RedisException e)
        {
            // If invalidation fails, the entry stays stale until it expires via TTL.
            logger.Warning(e, "Cache invalidation failed, entry stays stale until TTL expiry");
        }
    }
}
