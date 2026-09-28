using Search.Application.Products;
using Search.Application.Tests.Fakes;
using Search.Domain;

namespace Search.Application.Tests;

public sealed class UpdateProductTests
{
    [Fact]
    public async Task ExecuteAsync_SavesProductAndProductUpdatedEvent()
    {
        // Arrange
        var store = new FakeProductUpdateStore();
        var updateProduct = new UpdateProduct(store);

        var productId = Guid.NewGuid();

        var product = new Product
        {
            Id = productId,
            Name = "iPhone 17",
            Description = "Smartphone",
            Price = 999.99m,
            Category = "Phones"
        };

        // Act
        await updateProduct.ExecuteAsync(product);

        // Assert
        Assert.Equal(1, store.SaveCallCount);

        Assert.NotNull(store.Product);
        Assert.Equal(productId, store.Product.Id);
        Assert.Equal("iPhone 17", store.Product.Name);

        Assert.NotNull(store.ProductUpdated);
        Assert.Equal(productId, store.ProductUpdated.ProductId);
        Assert.Equal("iPhone 17", store.ProductUpdated.Name);
        Assert.Equal("Smartphone", store.ProductUpdated.Description);
        Assert.Equal(999.99m, store.ProductUpdated.Price);
        Assert.Equal("Phones", store.ProductUpdated.Category);

        Assert.True(
            store.ProductUpdated.OccurredAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStoreFails_PropagatesException()
    {
        // Arrange
        var store = new FakeProductUpdateStore
        {
            ShouldFail = true
        };

        var updateProduct = new UpdateProduct(store);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "iPhone 17",
            Description = "Smartphone",
            Price = 999.99m,
            Category = "Phones"
        };

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => updateProduct.ExecuteAsync(product));

        Assert.Equal(1, store.SaveCallCount);
    }
}