namespace Search.Infrastructure.Configuration;

public sealed class RedisOptions
{
    public string ConnectionString { get; init; } = string.Empty;

    public int CacheExpirationMinutes { get; init; } = 5;

}