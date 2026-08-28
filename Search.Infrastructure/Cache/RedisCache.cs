using Microsoft.Extensions.Options;
using Search.Application.Abstractions;
using Search.Infrastructure.Configuration;
using StackExchange.Redis;
using System.Text.Json;

namespace Search.Infrastructure.Cache;

public sealed class RedisCache<T> : ICache<T>
{
    private readonly IConnectionMultiplexer _connection;
    private readonly RedisOptions _options;

    public RedisCache(
        IConnectionMultiplexer connection,
        IOptions<RedisOptions> options)
    {
        _connection = connection;
        _options = options.Value;
    }

    public async Task<T?> GetAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var database = _connection.GetDatabase();

        var value = await database.StringGetAsync(key);

        if (value.IsNull)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(
            value.ToString());
    }

    public async Task SetAsync(
        string key,
        T value,
        CancellationToken cancellationToken = default)
    {
        var database = _connection.GetDatabase();

        var serializedValue = JsonSerializer.Serialize(value);

        await database.StringSetAsync(
            key,
            serializedValue,
            TimeSpan.FromMinutes(
                _options.CacheExpirationMinutes));
    }
}