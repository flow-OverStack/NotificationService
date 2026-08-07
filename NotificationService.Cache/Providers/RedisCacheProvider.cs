using Newtonsoft.Json;
using NotificationService.Domain.Interfaces.Provider;
using StackExchange.Redis;

namespace NotificationService.Cache.Providers;

public class RedisCacheProvider(IDatabase redisDatabase) : ICacheProvider
{
    private const string RedisErrorMessage = "An exception occurred while executing the Redis command.";

    public async Task<T?> GetJsonParsedAsync<T>(string key, CancellationToken cancellationToken = default)
    {   
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var value = await redisDatabase.StringGetAsync(key);
            if (value.IsNull) return default;

            return JsonConvert.DeserializeObject<T>(value.ToString());
        }
        catch (Exception)
        {
            // If deserialization fails, we treat it as a miss.
            return default;
        }
    }

    public async Task StringSetAsync<TValue>(string key, TValue value, int? timeToLiveInSeconds = null,
        bool fireAndForget = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var stringValue = value as string ?? JsonConvert.SerializeObject(value);

        var commandFlags = fireAndForget
            ? CommandFlags.FireAndForget
            : CommandFlags.None;

        cancellationToken.ThrowIfCancellationRequested();

        var result = await redisDatabase.StringSetAsync(key, stringValue,
            timeToLiveInSeconds != null ? TimeSpan.FromSeconds((int)timeToLiveInSeconds) : null,
            flags: commandFlags);

        if (!fireAndForget && !result)
            throw new RedisException(RedisErrorMessage);
    }

    public async Task<long> SetsAddAsync(string key, IEnumerable<string> values, int? timeToLiveInSeconds = null,
        bool fireAndForget = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(values);

        var redisValues = values.Select(x => new RedisValue(x)).ToArray();
        if (redisValues.Length == 0) return 0;

        var commandFlags = fireAndForget
            ? CommandFlags.FireAndForget
            : CommandFlags.None;

        cancellationToken.ThrowIfCancellationRequested();

        var added = await redisDatabase.SetAddAsync(key, redisValues, commandFlags);

        if (timeToLiveInSeconds == null) return added;
        
        cancellationToken.ThrowIfCancellationRequested();
        var expired = await redisDatabase.KeyExpireAsync(key, TimeSpan.FromSeconds((int)timeToLiveInSeconds),
            commandFlags);

        if (!fireAndForget && !expired)
            throw new RedisException(RedisErrorMessage);

        return added;
    }

    public async Task<IEnumerable<string>> SetStringMembersAsync(string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        cancellationToken.ThrowIfCancellationRequested();

        var values = (await redisDatabase.SetMembersAsync(key)).Select(x => x.ToString());
        return values;
    }

    public Task<long> KeysDeleteAsync(IEnumerable<string> keys, bool fireAndForget = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var commandFlags = fireAndForget
            ? CommandFlags.FireAndForget
            : CommandFlags.None;

        cancellationToken.ThrowIfCancellationRequested();

        return redisDatabase.KeyDeleteAsync(keys.Select(x => (RedisKey)x).ToArray(), commandFlags);
    }
}
