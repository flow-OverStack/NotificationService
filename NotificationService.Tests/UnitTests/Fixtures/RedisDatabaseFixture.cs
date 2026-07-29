using Moq;
using StackExchange.Redis;

namespace NotificationService.Tests.UnitTests.Fixtures;

internal static class RedisDatabaseFixture
{
    /// <summary>
    ///     An in-memory fake backing store, so writes made through the provider are visible to later reads
    ///     within the same test - a real round trip without a real Redis instance.
    /// </summary>
    public static IDatabase GetRedisDatabaseConfiguration()
    {
        var strings = new Dictionary<string, string>();
        var sets = new Dictionary<string, HashSet<string>>();
        var mockDatabase = new Mock<IDatabase>();

        mockDatabase.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue value, TimeSpan? _, bool _, When _, CommandFlags _) =>
            {
                strings[key.ToString()] = value.ToString();
                return true;
            });

        mockDatabase.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) =>
                strings.TryGetValue(key.ToString(), out var value) ? value : RedisValue.Null);

        mockDatabase.Setup(x => x.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue[] values, CommandFlags _) =>
            {
                if (!sets.TryGetValue(key.ToString(), out var set))
                    sets[key.ToString()] = set = [];

                return values.LongCount(value => set.Add(value.ToString()));
            });

        mockDatabase.Setup(x => x.SetMembersAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) =>
                sets.TryGetValue(key.ToString(), out var set)
                    ? [.. set.Select(value => (RedisValue)value)]
                    : []);

        mockDatabase.Setup(x => x.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        mockDatabase.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey[] keys, CommandFlags _) =>
            {
                return keys.Select(key => strings.Remove(key.ToString()) | sets.Remove(key.ToString())).LongCount(removed => removed);
            });

        return mockDatabase.Object;
    }

    /// <summary>
    ///     Every command throws, as if Redis were unreachable - used to prove the cache layer never lets a
    ///     Redis failure surface into the request.
    /// </summary>
    public static IDatabase GetThrowingRedisDatabaseConfiguration()
    {
        var exception = new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis unavailable");
        var mockDatabase = new Mock<IDatabase>();

        mockDatabase.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(exception);
        mockDatabase.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(exception);
        mockDatabase.Setup(x => x.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(exception);
        mockDatabase.Setup(x => x.SetMembersAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(exception);
        mockDatabase.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(exception);

        return mockDatabase.Object;
    }
}
