using Search.Application.Products;
using Search.Domain;

namespace Search.Application.Tests.Products;

public sealed class ProductSearchCacheKeyTests
{
    [Fact]
    public void Create_SameRequest_ReturnsSameKey()
    {
        // Arrange
        var request1 = new ProductSearchRequest
        {
            Query = "iphone",
            Page = 1,
            PageSize = 20
        };

        var request2 = new ProductSearchRequest
        {
            Query = "iphone",
            Page = 1,
            PageSize = 20
        };

        // Act
        var key1 = ProductSearchCacheKey.Create(request1);
        var key2 = ProductSearchCacheKey.Create(request2);

        // Assert
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void Create_DifferentPage_ReturnsDifferentKey()
    {
        // Arrange
        var request1 = new ProductSearchRequest
        {
            Query = "iphone",
            Page = 1,
            PageSize = 20
        };

        var request2 = new ProductSearchRequest
        {
            Query = "iphone",
            Page = 2,
            PageSize = 20
        };

        // Act
        var key1 = ProductSearchCacheKey.Create(request1);
        var key2 = ProductSearchCacheKey.Create(request2);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void Create_DifferentQuery_ReturnsDifferentKey()
    {
        // Arrange
        var request1 = new ProductSearchRequest
        {
            Query = "iphone",
            Page = 1,
            PageSize = 20
        };

        var request2 = new ProductSearchRequest
        {
            Query = "samsung",
            Page = 1,
            PageSize = 20
        };

        // Act
        var key1 = ProductSearchCacheKey.Create(request1);
        var key2 = ProductSearchCacheKey.Create(request2);

        // Assert
        Assert.NotEqual(key1, key2);
    }
}