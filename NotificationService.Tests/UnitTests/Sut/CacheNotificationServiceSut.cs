using Microsoft.Extensions.Options;
using NotificationService.Application.Services.Cache;
using NotificationService.Cache.Providers;
using NotificationService.Cache.Repositories;
using NotificationService.Domain.Interface.Repository.Cache;
using NotificationService.Domain.Interface.Service;
using NotificationService.Tests.UnitTests.Fixtures;
using StackExchange.Redis;

namespace NotificationService.Tests.UnitTests.Sut;

internal class CacheNotificationServiceSut
{
    private readonly CacheNotificationEventHandler _cacheNotificationEventHandler;
    private readonly CacheNotificationService _cacheNotificationService;

    public readonly INotificationCacheRepository CacheRepository;
    public readonly NotificationServiceSut InnerSut;

    public CacheNotificationServiceSut(IDatabase? database = null)
    {
        InnerSut = new NotificationServiceSut();
        CacheRepository = new NotificationCacheRepository(
            new RedisCacheProvider(database ?? RedisDatabaseFixture.GetRedisDatabaseConfiguration()),
            Options.Create(RedisSettingsFixture.GetRedisSettingsConfiguration()));

        _cacheNotificationService = new CacheNotificationService(CacheRepository, InnerSut.GetService());
        _cacheNotificationEventHandler =
            new CacheNotificationEventHandler(CacheRepository, InnerSut.GetEventHandler());
    }

    public INotificationService GetService()
    {
        return _cacheNotificationService;
    }

    public INotificationEventHandler GetEventHandler()
    {
        return _cacheNotificationEventHandler;
    }
}
