using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using Search.Domain;
using Search.Infrastructure.Configuration;

namespace Search.Infrastructure.Search;

public sealed class ProductSeeder
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;

    public ProductSeeder(
        ElasticsearchClient client,
        IOptions<ElasticsearchOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        var countResponse = await _client.CountAsync<Product>(
            c => c
                .Indices(_options.ProductIndex),
            cancellationToken);

        if (countResponse.Count > 0)
        {
            return;
        }

        var products = new[]
        {
            new Product
            {
                Id = Guid.Parse("285484d3-f209-4fca-b44a-28a1bda02d67"),
                Name = "iPhone 17 Pro",
                Description = "Флагманский смартфон Apple",
                Price = 129999,
                Category = "Смартфоны"
            },
            new Product
            {
                Id = Guid.Parse("385484d3-f209-4fca-b44a-28a1bda02d67"),
                Name = "Samsung Galaxy S26",
                Description = "Флагманский смартфон Samsung",
                Price = 99999,
                Category = "Смартфоны"
            },
            new Product
            {
                Id = Guid.Parse("485484d3-f209-4fca-b44a-28a1bda02d67"),
                Name = "MacBook Pro",
                Description = "Ноутбук Apple",
                Price = 249999,
                Category = "Ноутбуки"
            },
            new Product
            {
                Id = Guid.Parse("585484d3-f209-4fca-b44a-28a1bda02d67"),
                Name = "Apple Watch",
                Description = "Умные часы",
                Price = 59999,
                Category = "Смарт-часы"
            }

        };

        foreach (var product in products)
        {
            var response = await _client.IndexAsync(
                product,
                i => i
                    .Index(_options.ProductIndex)
                    .Id(product.Id),
                cancellationToken);

            if (!response.IsValidResponse)
            {
                throw new InvalidOperationException(
                    $"Failed to index product {product.Id}.");
            }
        }
    }
}