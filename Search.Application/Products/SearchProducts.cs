using Search.Application.Abstractions;
using Search.Domain;

namespace Search.Application.Products;

public sealed class SearchProducts
{
    private readonly IProductSearchRepository _repository;
    private readonly ICache<ProductSearchResult> _cache;

    public SearchProducts(
        IProductSearchRepository repository,
        ICache<ProductSearchResult> cache)
    {
        _repository = repository;
        _cache = cache;
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
            Console.WriteLine("CACHE HIT");

            return cachedResult;
        }

        Console.WriteLine("CACHE MISS");

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