using System.Text.Json;
using Drive.Application.Interfaces;
using StackExchange.Redis;

namespace Drive.Infrastructure.Caching;

public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        if (value is null)
            return;

        string serialized = value is string str ? str : JsonSerializer.Serialize(value);
        Expiration expiration = expiry.HasValue ? (Expiration)expiry.Value : default;

        await _database.StringSetAsync(key, serialized, expiration);
    }

    public async Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(key);
        if (value.IsNullOrEmpty)
            return default;

        var str = value.ToString();
        if (typeof(T) == typeof(string))
            return (T)(object)str;

        return JsonSerializer.Deserialize<T>(str);
    }

    public async Task<T?> GetAndDeleteAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetDeleteAsync(key);
        if (value.IsNullOrEmpty)
            return default;

        var str = value.ToString();
        if (typeof(T) == typeof(string))
            return (T)(object)str;

        return JsonSerializer.Deserialize<T>(str);
    }

    public async Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return await _database.KeyExistsAsync(key);
    }
}
