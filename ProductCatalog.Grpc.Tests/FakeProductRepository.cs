using Search.Application.Abstractions;
using Search.Domain;

namespace ProductCatalog.Grpc.Tests;

internal sealed class FakeProductRepository
    : IProductRepository
{
    public Product? ProductToReturn { get; set; }

    public Guid? RequestedId { get; private set; }

    public CancellationToken ReceivedCancellationToken
    {
        get;
        private set;
    }

    public Func<Guid, CancellationToken, Task<Product?>>?
    GetByIdHandler
    { get; set; }

    public Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        RequestedId = id;
        ReceivedCancellationToken = cancellationToken;

        if (GetByIdHandler is not null)
        {
            return GetByIdHandler(
                id,
                cancellationToken);
        }

        return Task.FromResult(ProductToReturn);
    }

    public Task UpsertAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}