using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using Search.Application.Abstractions;
using Search.Infrastructure.Outbox;
using Search.Infrastructure.Persistence;

namespace Search.Infrastructure.Configuration;

public static class MongoServiceCollectionExtensions
{
    public static IServiceCollection AddMongoPersistence(
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

            var mongoUrl =
                MongoUrl.Create(options.ConnectionString);

            var settings =
                MongoClientSettings.FromUrl(mongoUrl);

            settings.ClusterConfigurator = cb =>
                cb.Subscribe(
                    new DiagnosticsActivityEventSubscriber());

            return new MongoClient(settings);
        });

        services.AddScoped<
            IProductUpdateStore,
            MongoProductUpdateStore>();

        services.AddScoped<
            IProductRepository,
            MongoProductRepository>();

        return services;
    }

    public static IServiceCollection AddOutboxProcessing(
    this IServiceCollection services)
    {
        services.AddSingleton<OutboxProcessor>();
        services.AddHostedService<OutboxPublisher>();
        services.AddHostedService<OutboxMetricsCollector>();

        return services;
    }

    public static IServiceCollection AddMongo(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMongoPersistence(configuration);
        services.AddOutboxProcessing();

        return services;
    }
}