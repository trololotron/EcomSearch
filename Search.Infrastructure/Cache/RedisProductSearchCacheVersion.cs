using Search.Application.Abstractions;
using Search.Infrastructure.Diagnostics;
using StackExchange.Redis;

namespace Search.Infrastructure.Redis;

public sealed class RedisProductSearchCacheVersion
    : IProductSearchCacheVersion
{
    private const string VersionKey = "search:products:version";

    private readonly IConnectionMultiplexer _connection;

    public RedisProductSearchCacheVersion(
        IConnectionMultiplexer connection)
    {
        _connection = connection;
    }

    public async Task<long> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var database = _connection.GetDatabase();

        var value = await database.StringGetAsync(VersionKey);

        if (value.IsNull)
        {
            return 0;
        }

        return (long)value;
    }

    public async Task<long> IncrementAsync(
        CancellationToken cancellationToken = default)
    {
        using var activity =
            InfrastructureTelemetry.ActivitySource.StartActivity("Redis.CacheVersionIncrement");

        activity?.SetTag(
            "redis.key",
            "search:products:version");

        var database = _connection.GetDatabase();

        return await database.StringIncrementAsync(VersionKey);
    }
}