using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Search.Application.Abstractions;
using Search.Infrastructure.Search;

namespace Search.Infrastructure.Configuration;

public static class ElasticsearchServiceCollectionExtensions
{
    public static IServiceCollection AddElasticsearch(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<ElasticsearchOptions>()
            .Bind(configuration.GetSection(
                ElasticsearchConfiguration.SectionName))
            .Validate(options =>
                Uri.TryCreate(
                    options.Url,
                    UriKind.Absolute,
                    out _),
                "Elasticsearch Url is invalid.")
            .Validate(options =>
                !string.IsNullOrWhiteSpace(
                    options.ProductIndex),
                "Elasticsearch ProductIndex is required.")
            .ValidateOnStart();

        services
            .AddOptions<ProductSearchOptions>()
            .Bind(configuration.GetSection(ProductSearchOptions.SectionName))
            .Validate(options => options.NameBoost > 0,
                "ProductSearch NameBoost must be greater than zero.")
            .Validate(options => options.DescriptionBoost > 0,
                "ProductSearch DescriptionBoost must be greater than zero.")
            .Validate(options => options.CategoryBoost > 0,
                "ProductSearch CategoryBoost must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp
                .GetRequiredService<IOptions<ElasticsearchOptions>>()
                .Value;

            return new ElasticsearchClient(
                new ElasticsearchClientSettings(
                    new Uri(options.Url)));
        });

        services.AddSingleton<ElasticsearchIndexInitializer>();

        services.AddScoped<
            IProductSearchRepository,
            ElasticsearchProductSearchRepository>();

        services.AddSingleton<ProductSeeder>();

        services.AddHostedService<ElasticsearchStartupService>();

        return services;
    }
}