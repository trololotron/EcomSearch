using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Search.Application.Events;
using Search.Application.Tests.Fakes;
using Search.Infrastructure.Messaging;

namespace Search.Application.Tests.Events;

public sealed class ProductUpdatedHandlerTests
{
    [Fact]
    public async Task HandleAsync_IndexesProduct_AndIncrementsCacheVersion()
    {
        // Arrange
        var indexWriter = new FakeProductIndexWriter();

        var cacheVersion = new FakeProductSearchCacheVersion
        {
            Version = 10
        };

        var handler = new ProductUpdatedHandler(
            indexWriter,
            cacheVersion, NullLogger<ProductUpdatedHandler>.Instance);

        var productId = Guid.NewGuid();

        var message = new ProductUpdated(
            productId,
            "iPhone 17",
            "Smartphone",
            999.99m,
            "Phones",
            DateTimeOffset.UtcNow);

        // Act
        await handler.HandleAsync(message);

        // Assert
        Assert.Equal(1, indexWriter.UpsertCallCount);

        Assert.NotNull(indexWriter.Product);

        Assert.Equal(productId, indexWriter.Product.Id);
        Assert.Equal("iPhone 17", indexWriter.Product.Name);
        Assert.Equal("Smartphone", indexWriter.Product.Description);
        Assert.Equal(999.99m, indexWriter.Product.Price);
        Assert.Equal("Phones", indexWriter.Product.Category);

        Assert.Equal(11, cacheVersion.Version);
    }

    [Fact]
    public async Task HandleAsync_WhenIndexingFails_DoesNotIncrementCacheVersion()
    {
        // Arrange
        var indexWriter = new FakeProductIndexWriter
        {
            ShouldFail = true
        };

        var cacheVersion = new FakeProductSearchCacheVersion
        {
            Version = 10
        };

        var handler = new ProductUpdatedHandler(
            indexWriter,
            cacheVersion, NullLogger<ProductUpdatedHandler>.Instance);

        var message = new ProductUpdated(
            Guid.NewGuid(),
            "iPhone 17",
            "Smartphone",
            999.99m,
            "Phones",
            DateTimeOffset.UtcNow);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(message));

        // Assert
        Assert.Equal(1, indexWriter.UpsertCallCount);
        Assert.Equal(10, cacheVersion.Version);
    }

    [Fact]
    public async Task HandleAsync_WhenCacheVersionIncrementFails_DoesNotFail()
    {
        // Arrange
        var indexWriter = new FakeProductIndexWriter();

        var cacheVersion = new FakeProductSearchCacheVersion
        {
            Version = 10,
            FailOnIncrement = true
        };

        var handler = new ProductUpdatedHandler(
            indexWriter,
            cacheVersion,
            NullLogger<ProductUpdatedHandler>.Instance);

        var productId = Guid.NewGuid();

        var message = new ProductUpdated(
            productId,
            "iPhone 17",
            "Smartphone",
            999.99m,
            "Phones",
            DateTimeOffset.UtcNow);

        // Act
        await handler.HandleAsync(message);

        // Assert
        Assert.Equal(1, indexWriter.UpsertCallCount);

        Assert.NotNull(indexWriter.Product);
        Assert.Equal(productId, indexWriter.Product.Id);

        Assert.Equal(10, cacheVersion.Version);
    }
}