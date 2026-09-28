using Microsoft.Extensions.Logging.Abstractions;
using Search.Application.Abstractions;
using Search.Application.Products;
using Search.Application.Tests.Fakes;
using Search.Domain;

namespace Search.Application.Tests.Products;

public sealed class SearchProductsTests
{
    private static SearchProducts CreateSearchProducts(
        IProductSearchRepository repository,
        ICache<ProductSearchResult> cache,
        IProductSearchCacheVersion? cacheVersion = null)
    {
        cacheVersion ??= new FakeProductSearchCacheVersion
        {
            Version = 1
        };

        return new SearchProducts(
            repository,
            cache,
            NullLogger<SearchProducts>.Instance,
            cacheVersion);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsProductsFromRepository()
    {
        // Arrange
        var repository = new FakeProductSearchRepository();
        var cache = new FakeCache<ProductSearchResult>();

        var searchProducts = CreateSearchProducts(
         repository,
         cache);

        // Act
        var result = await searchProducts.ExecuteAsync(
            new ProductSearchRequest
            {
                Query = "iphone"
            });

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("iPhone 17 Pro", result.Items[0].Name);
        Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task ExecuteAsync_PassesQueryToRepository()
    {
        // Arrange
        var repository = new FakeProductSearchRepository();
        var cache = new FakeCache<ProductSearchResult>();

        var searchProducts = CreateSearchProducts(
            repository,
            cache);

        // Act
        await searchProducts.ExecuteAsync(
            new ProductSearchRequest
            {
                Query = "iphone"
            });

        // Assert
        Assert.Equal("iphone", repository.LastQuery);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationTokenToRepository()
    {
        // Arrange
        var repository = new FakeProductSearchRepository();
        var cache = new FakeCache<ProductSearchResult>();
        var searchProducts = CreateSearchProducts(
            repository,
            cache);

        using var cts = new CancellationTokenSource();

        // Act
        await searchProducts.ExecuteAsync(
            new ProductSearchRequest
            {
                Query = "iphone"
            },
            cts.Token);

        // Assert
        Assert.Equal(
            cts.Token,
            repository.LastCancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsCachedResultWithoutCallingRepository()
    {
        // Arrange
        var repository = new FakeProductSearchRepository();
        var cache = new FakeCache<ProductSearchResult>();

        var request = new ProductSearchRequest
        {
            Query = "iphone"
        };

        var cachedResult = new ProductSearchResult
        {
            Items =
            [
                new Product
            {
                Id = Guid.NewGuid(),
                Name = "Cached iPhone",
                Description = "From cache",
                Price = 99999,
                Category = "Smartphones"
            }
            ],
            Total = 1,
            Page = 1,
            PageSize = 20
        };

        var cacheKey = ProductSearchCacheKey.Create(request, 1);

        await cache.SetAsync(
            cacheKey,
            cachedResult);

        var searchProducts = CreateSearchProducts(
            repository,
            cache);

        // Act
        var result = await searchProducts.ExecuteAsync(request);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Cached iPhone", result.Items[0].Name);
        Assert.Equal(1, result.Total);

        Assert.Equal(1, cache.GetCallCount);
        Assert.Equal(0, repository.SearchCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCacheMiss_ReturnsRepositoryResultAndCachesIt()
    {
        // Arrange
        var repository = new FakeProductSearchRepository();
        var cache = new FakeCache<ProductSearchResult>();

        var request = new ProductSearchRequest
        {
            Query = "iphone"
        };

        var searchProducts = CreateSearchProducts(
            repository,
            cache);

        // Act
        var result = await searchProducts.ExecuteAsync(request);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("iPhone 17 Pro", result.Items[0].Name);
        Assert.Equal(1, result.Total);

        Assert.Equal(1, repository.SearchCallCount);
        Assert.Equal(1, cache.GetCallCount);
        Assert.Equal(1, cache.SetCallCount);

        var cacheKey = ProductSearchCacheKey.Create(request, 1);

        var cachedValue = await cache.GetAsync(cacheKey);

        Assert.NotNull(cachedValue);
        Assert.Equal(result.Total, cachedValue.Total);
        Assert.Equal(result.Page, cachedValue.Page);
        Assert.Equal(result.PageSize, cachedValue.PageSize);
        Assert.Equal(
            result.Items[0].Name,
            cachedValue.Items[0].Name);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCacheGetFails_SearchesRepository()
    {
        // Arrange
        var repository = new FakeProductSearchRepository();

        var cache = new FailingCache<ProductSearchResult>(
            new InvalidOperationException("Redis is unavailable."),
            failOnGet: true);

        var searchProducts = CreateSearchProducts(
            repository,
            cache);

        var request = new ProductSearchRequest
        {
            Query = "iphone"
        };

        // Act
        var result = await searchProducts.ExecuteAsync(request);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("iPhone 17 Pro", result.Items[0].Name);
        Assert.Equal(1, repository.SearchCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCacheSetFails_ReturnsRepositoryResult()
    {
        // Arrange
        var repository = new FakeProductSearchRepository();

        var cache = new FailingCache<ProductSearchResult>(
            new InvalidOperationException("Redis is unavailable."),
            failOnSet: true);

        var searchProducts = CreateSearchProducts(
            repository,
            cache);

        var request = new ProductSearchRequest
        {
            Query = "iphone"
        };

        // Act
        var result = await searchProducts.ExecuteAsync(request);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("iPhone 17 Pro", result.Items[0].Name);
        Assert.Equal(1, repository.SearchCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCacheGetIsCanceled_PropagatesCancellation()
    {
        // Arrange
        var repository = new FakeProductSearchRepository();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var cache = new FailingCache<ProductSearchResult>(
            new OperationCanceledException(cts.Token),
            failOnGet: true);

        var searchProducts = CreateSearchProducts(
            repository,
            cache);

        var request = new ProductSearchRequest
        {
            Query = "iphone"
        };

        // Act
        var exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => searchProducts.ExecuteAsync(
                request,
                cts.Token));

        // Assert
        Assert.Equal(cts.Token, exception.CancellationToken);
        Assert.Equal(0, repository.SearchCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCacheVersionGetFails_BypassesCacheAndSearchesRepository()
    {
        // Arrange
        var repository = new FakeProductSearchRepository();
        var cache = new FakeCache<ProductSearchResult>();

        var cacheVersion = new FakeProductSearchCacheVersion
        {
            FailOnGet = true
        };

        var searchProducts = CreateSearchProducts(
            repository,
            cache,
            cacheVersion);

        var request = new ProductSearchRequest
        {
            Query = "iphone"
        };

        // Act
        var result = await searchProducts.ExecuteAsync(request);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("iPhone 17 Pro", result.Items[0].Name);

        Assert.Equal(1, repository.SearchCallCount);

        Assert.Equal(0, cache.GetCallCount);
        Assert.Equal(0, cache.SetCallCount);
    }
}