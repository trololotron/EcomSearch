using Search.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Search.Application.Products;

public sealed class SearchProducts
{
    private readonly IProductSearchRepository _repository;
    private readonly ICache<ProductSearchResult> _cache;

    private readonly ILogger<SearchProducts> _logger;

    public SearchProducts(
        IProductSearchRepository repository,
        ICache<ProductSearchResult> cache,
        ILogger<SearchProducts> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ProductSearchResult> ExecuteAsync(
        ProductSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = ProductSearchCacheKey.Create(request);

        var cachedResult = await _cache.GetAsync(
            cacheKey,
            cancellationToken);

        if (cachedResult is not null)
        {
            _logger.LogInformation("Cache HIT for key {CacheKey}", cacheKey);

            return cachedResult;
        }

        _logger.LogInformation("Cache MISS for key {CacheKey}", cacheKey);

        var result = await _repository.SearchAsync(
            request,
            cancellationToken);

        await _cache.SetAsync(
            cacheKey,
            result,
            cancellationToken);

        return result;
    }
}