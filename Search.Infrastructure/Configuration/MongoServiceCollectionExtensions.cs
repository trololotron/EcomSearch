using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Search.Application.Abstractions;
using Search.Infrastructure.Outbox;
using Search.Infrastructure.Persistence;

namespace Search.Infrastructure.Configuration;

public static class MongoServiceCollectionExtensions
{
    public static IServiceCollection AddMongo(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        MongoSerializationConfiguration.Configure();

        services
            .AddOptions<MongoOptions>()
            .Bind(configuration.GetSection("Mongo"))
            .Validate(
                options => !string.IsNullOrWhiteSpace(
                    options.ConnectionString),
                "Mongo ConnectionString is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(
                    options.DatabaseName),
                "Mongo DatabaseName is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(
                    options.ProductsCollection),
                "Mongo ProductsCollection is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(
                    options.OutboxCollection),
                "Mongo OutboxCollection is required.")
            .ValidateOnStart();

        services.AddSingleton<IMongoClient>(sp =>
        {
            var options = sp
                .GetRequiredService<IOptions<MongoOptions>>()
                .Value;

            return new MongoClient(
                options.ConnectionString);
        });

        services.AddSingleton<OutboxProcessor>();
        services.AddHostedService<OutboxPublisher>();

        services.AddScoped<
            IProductUpdateStore,
            MongoProductUpdateStore>();

        services.AddScoped<
            IProductRepository,
            MongoProductRepository>();

        return services;
    }
}