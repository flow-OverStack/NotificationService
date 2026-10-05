using Microsoft.Extensions.Options;
using NotificationService.Cache.Extensions;
using NotificationService.Cache.Helpers;
using NotificationService.Cache.Settings;
using NotificationService.Domain.Dtos.Notification;
using NotificationService.Domain.Interfaces.Provider;
using NotificationService.Domain.Interfaces.Repository.Cache;
using Serilog;

namespace NotificationService.Cache.Repositories;

public class NotificationCacheRepository(ICacheProvider cache, IOptions<RedisSettings> redisSettings, ILogger logger)
    : INotificationCacheRepository
{
    private readonly int _timeToLiveInSeconds = redisSettings.Value.TimeToLiveInSeconds;

    public async Task<IEnumerable<NotificationDto>?> GetAsync(long recipientId, bool unreadOnly, int? skip,
        int? take, CancellationToken cancellationToken = default)
    {
        var key = CacheKeyHelper.GetRecipientNotificationsKey(recipientId, unreadOnly, skip, take);

        try
        {
            return await cache.GetJsonParsedAsync<NotificationDto[]>(key, cancellationToken);
        }
        catch (Exception e) when (e.IsRedisFailure())
        {
            // If reading from the cache fails, we treat it as a miss.
            logger.LogRedisFailure(e, [key]);
            return null;
        }
    }

    public async Task SetAsync(long recipientId, bool unreadOnly, int? skip, int? take,
        IEnumerable<NotificationDto> notifications, CancellationToken cancellationToken = default)
    {
        var key = CacheKeyHelper.GetRecipientNotificationsKey(recipientId, unreadOnly, skip, take);
        var indexKey = CacheKeyHelper.GetRecipientNotificationKeysKey(recipientId);

        try
        {
            await cache.StringSetAsync(key, notifications.ToArray(), _timeToLiveInSeconds, true,
                CancellationToken.None);
            await cache.SetsAddAsync(indexKey, [key], _timeToLiveInSeconds, true, CancellationToken.None);
        }
        catch (Exception e) when (e.IsRedisFailure())
        {
            // If caching fails, we still return the fetched data without caching it.
            logger.LogRedisFailure(e, [key, indexKey]);
        }
    }

    public async Task InvalidateAsync(long recipientId, CancellationToken cancellationToken = default)
    {
        var indexKey = CacheKeyHelper.GetRecipientNotificationKeysKey(recipientId);

        try
        {
            var pageKeys = (await cache.SetStringMembersAsync(indexKey, CancellationToken.None)).ToArray();

            await cache.KeysDeleteAsync([.. pageKeys, indexKey], true, CancellationToken.None);
        }
        catch (Exception e) when (e.IsRedisFailure())
        {
            // If invalidation fails, the entry stays stale until it expires via TTL.
            logger.LogRedisFailure(e, [indexKey]);
        }
    }
}