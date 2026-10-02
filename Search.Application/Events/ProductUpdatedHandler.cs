using Microsoft.Extensions.Logging;
using Search.Application.Abstractions;
using Search.Application.Diagnostics;
using Search.Domain;

namespace Search.Application.Events;

public sealed class ProductUpdatedHandler
    : IProductUpdatedHandler
{
    private readonly IProductIndexWriter _indexWriter;
    private readonly IProductSearchCacheVersion _cacheVersion;
    private readonly ILogger<ProductUpdatedHandler> _logger;

    public ProductUpdatedHandler(
        IProductIndexWriter indexWriter,
        IProductSearchCacheVersion cacheVersion,
        ILogger<ProductUpdatedHandler> logger)
    {
        _indexWriter = indexWriter;
        _cacheVersion = cacheVersion;
        _logger = logger;
    }

    public async Task HandleAsync(
        ProductUpdated message,
        CancellationToken cancellationToken = default)
    {
        using var activity =
            ApplicationTelemetry.ActivitySource.StartActivity(
                "ProductUpdatedHandler.Handle");

        activity?.SetTag("product.id", message.ProductId);


        var product = new Product
        {
            Id = message.ProductId,
            Name = message.Name,
            Description = message.Description,
            Price = message.Price,
            Category = message.Category
        };

        await _indexWriter.UpsertAsync(
            product,
            cancellationToken);

        try
        {
            await _cacheVersion.IncrementAsync(
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to invalidate search cache after updating product {ProductId}. Cached data may remain stale until expiration.",
                product.Id);
        }
    }
}