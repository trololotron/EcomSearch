using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using Search.Domain;
using Search.Infrastructure.Configuration;

namespace Search.Infrastructure.Search;

public sealed class ElasticsearchIndexInitializer
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;

    public ElasticsearchIndexInitializer(
        ElasticsearchClient client,
        IOptions<ElasticsearchOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        var existsResponse = await _client.Indices.ExistsAsync(
            _options.ProductIndex,
            cancellationToken);

        if (existsResponse.Exists)
        {
            return;
        }

        await _client.Indices.CreateAsync<Product>(
            c => c
                .Index(_options.ProductIndex)
                .Mappings(m => m
                    .Properties(p => p
                        .Keyword(x => x.Id)
                        .Text(x => x.Name)
                        .Text(x => x.Description)
                        .DoubleNumber(x => x.Price)
                        .Keyword(x => x.Category))),
            cancellationToken);
    }
}