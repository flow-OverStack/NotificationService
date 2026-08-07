using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Application.Services.Cache;
using NotificationService.Cache.Providers;
using NotificationService.Cache.Repositories;
using NotificationService.Domain.Interface.Repository.Cache;
using NotificationService.Domain.Interface.Service;
using NotificationService.Tests.UnitTests.Fixtures;
using Serilog;
using StackExchange.Redis;

namespace NotificationService.Tests.UnitTests.Sut;

internal class CacheNotificationServiceSut
{
    private readonly CacheNotificationEventHandler _cacheNotificationEventHandler;
    private readonly CacheNotificationService _cacheNotificationService;

    public readonly INotificationCacheRepository CacheRepository;
    public readonly NotificationServiceSut InnerSut;
    public readonly Mock<ILogger> LoggerMock = new();

    public CacheNotificationServiceSut(IDatabase? database = null)
    {
        InnerSut = new NotificationServiceSut();
        CacheRepository = new NotificationCacheRepository(
            new RedisCacheProvider(database ?? RedisDatabaseFixture.GetRedisDatabaseConfiguration()),
            Options.Create(RedisSettingsFixture.GetRedisSettingsConfiguration()),
            LoggerMock.Object);

        _cacheNotificationService =
            new CacheNotificationService(CacheRepository, InnerSut.PaginationResolver, InnerSut.GetService());
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
