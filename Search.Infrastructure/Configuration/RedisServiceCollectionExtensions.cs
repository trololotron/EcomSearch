using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Search.Application.Abstractions;
using Search.Infrastructure.Cache;
using StackExchange.Redis;

namespace Search.Infrastructure.Configuration;

public static class RedisServiceCollectionExtensions
{
    public static IServiceCollection AddRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<RedisOptions>()
            .Bind(configuration.GetSection("Redis"))
            .Validate(
                options => !string.IsNullOrWhiteSpace(
                    options.ConnectionString),
                "Redis ConnectionString is required.")
            .ValidateOnStart();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp
                .GetRequiredService<IOptions<RedisOptions>>()
                .Value;

            return ConnectionMultiplexer.Connect(
                options.ConnectionString);
        });

        services.AddSingleton(
            typeof(ICache<>),
            typeof(RedisCache<>));

        return services;
    }
}