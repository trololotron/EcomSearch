using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Search.Application.Abstractions;
using Search.Domain;
using Search.Infrastructure.Configuration;

namespace Search.Infrastructure.Search;

public sealed class ElasticsearchProductIndexWriter
    : IProductIndexWriter
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<ElasticsearchProductIndexWriter> _logger;

    public ElasticsearchProductIndexWriter(
        ElasticsearchClient client,
        IOptions<ElasticsearchOptions> options,
        ILogger<ElasticsearchProductIndexWriter> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task UpsertAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.IndexAsync(
            product,
            index => index
                .Index(_options.ProductIndex)
                .Id(product.Id),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            _logger.LogError(
                "Failed to index product {ProductId}. DebugInformation: {DebugInformation}",
                product.Id,
                response.DebugInformation);

            throw new InvalidOperationException(
                "Failed to index product in Elasticsearch.");
        }

        _logger.LogInformation(
            "Product {ProductId} indexed in Elasticsearch.",
            product.Id);
    }
}