namespace NotificationService.Domain.Interface.Provider;

public interface ICacheProvider
{
    /// <summary>
    ///     Gets the value stored at the given key and deserializes it from JSON.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during the operation.</param>
    /// <returns>The deserialized value, or <c>default</c> if the key is missing or deserialization fails.</returns>
    Task<T?> GetJsonParsedAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the value for the given key, serialized to JSON, optionally with a time-to-live.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to be stored. Values are serialized to JSON.</typeparam>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="timeToLiveInSeconds">The optional time-to-live (TTL) for the key in seconds.</param>
    /// <param name="fireAndForget">If true, sends the command in fire-and-forget mode (no result or error reported).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during the operation.</param>
    Task StringSetAsync<TValue>(string key, TValue value, int? timeToLiveInSeconds = null,
        bool fireAndForget = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds the given values to the set stored at the given key. Optionally sets a time-to-live for the key.
    /// </summary>
    /// <param name="key">The key identifying the set.</param>
    /// <param name="values">The members to add to the set.</param>
    /// <param name="timeToLiveInSeconds">The optional time-to-live (TTL) for the key in seconds.</param>
    /// <param name="fireAndForget">If true, sends the command in fire-and-forget mode (no result or error reported).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during the operation.</param>
    /// <returns>The number of members added to the set.</returns>
    Task<long> SetsAddAsync(string key, IEnumerable<string> values, int? timeToLiveInSeconds = null,
        bool fireAndForget = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all members of the set stored at the given key.
    /// </summary>
    /// <param name="key">The key identifying the set.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during the operation.</param>
    Task<IEnumerable<string>> SetStringMembersAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes the given keys from the cache.
    /// </summary>
    /// <param name="keys">The keys to delete.</param>
    /// <param name="fireAndForget">If true, sends the command in fire-and-forget mode (no result or error reported).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during the operation.</param>
    /// <returns>The number of keys that were removed.</returns>
    Task<long> KeysDeleteAsync(IEnumerable<string> keys, bool fireAndForget = false,
        CancellationToken cancellationToken = default);
}
