using Search.Application.Abstractions;
using Search.Domain;

namespace Search.Application.Tests.Fakes;

public sealed class FakeProductIndexWriter
    : IProductIndexWriter
{
    public Product? Product { get; private set; }

    public int UpsertCallCount { get; private set; }

    public bool ShouldFail { get; set; }

    public Task UpsertAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        UpsertCallCount++;

        if (ShouldFail)
        {
            throw new InvalidOperationException(
                "Simulated Elasticsearch failure.");
        }

        Product = product;

        return Task.CompletedTask;
    }
}