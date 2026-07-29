using NotificationService.Cache.Settings;

namespace NotificationService.Tests.UnitTests.Fixtures;

internal static class RedisSettingsFixture
{
    public static RedisSettings GetRedisSettingsConfiguration()
    {
        return new RedisSettings { TimeToLiveInSeconds = 150 };
    }
}
