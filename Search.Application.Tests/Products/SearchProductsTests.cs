using Microsoft.Extensions.Logging.Abstractions;
using Search.Application.Products;
using Search.Application.Tests.Fakes;
using Search.Domain;

namespace Search.Application.Tests.Products;

public sealed class SearchProductsTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsProductsFromRepository()
    {
        // Arrange
        var repository = new FakeProductSearchRepository();
        var cache = new FakeCache<ProductSearchResult>();

        var searchProducts = new SearchProducts(
            repository,
            cache,
            NullLogger<SearchProducts>.Instance);

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

        var searchProducts = new SearchProducts(
            repository,
            cache,
            NullLogger<SearchProducts>.Instance);

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
        var searchProducts = new SearchProducts(
            repository,
            cache,
            NullLogger<SearchProducts>.Instance);

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

        var cacheKey = ProductSearchCacheKey.Create(request);

        await cache.SetAsync(
            cacheKey,
            cachedResult);

        var searchProducts = new SearchProducts(
            repository,
            cache,
            NullLogger<SearchProducts>.Instance);

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

        var searchProducts = new SearchProducts(
            repository,
            cache,
            NullLogger<SearchProducts>.Instance);

        // Act
        var result = await searchProducts.ExecuteAsync(request);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("iPhone 17 Pro", result.Items[0].Name);
        Assert.Equal(1, result.Total);

        Assert.Equal(1, repository.SearchCallCount);
        Assert.Equal(1, cache.GetCallCount);
        Assert.Equal(1, cache.SetCallCount);

        var cacheKey = ProductSearchCacheKey.Create(request);

        var cachedValue = await cache.GetAsync(cacheKey);

        Assert.NotNull(cachedValue);
        Assert.Equal(result.Total, cachedValue.Total);
        Assert.Equal(result.Page, cachedValue.Page);
        Assert.Equal(result.PageSize, cachedValue.PageSize);
        Assert.Equal(
            result.Items[0].Name,
            cachedValue.Items[0].Name);
    }
}