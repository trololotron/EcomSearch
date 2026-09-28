using Search.Application.Abstractions;
using Search.Application.Events;
using Search.Domain;

namespace Search.Application.Tests.Fakes;

public sealed class FakeProductUpdateStore
    : IProductUpdateStore
{
    public Product? Product { get; private set; }

    public ProductUpdated? ProductUpdated { get; private set; }

    public int SaveCallCount { get; private set; }

    public bool ShouldFail { get; set; }

    public Task SaveAsync(
        Product product,
        ProductUpdated productUpdated,
        CancellationToken cancellationToken = default)
    {
        SaveCallCount++;

        if (ShouldFail)
        {
            throw new InvalidOperationException(
                "Product update store failed.");
        }

        Product = product;
        ProductUpdated = productUpdated;

        return Task.CompletedTask;
    }
}