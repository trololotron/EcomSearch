using Search.Application.Abstractions;
using Search.Domain;

namespace Search.Application.Tests.Fakes;

public sealed class FakeProductRepository
    : IProductRepository
{
    public Product? Product { get; private set; }

    public int UpsertCallCount { get; private set; }

    public bool ShouldFail { get; set; }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpsertAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        UpsertCallCount++;

        if (ShouldFail)
        {
            throw new InvalidOperationException(
                "MongoDB failed.");
        }

        Product = product;

        return Task.CompletedTask;
    }
}