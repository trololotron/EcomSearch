using Search.Domain;

namespace Search.Application.Abstractions;

public interface IProductRepository
{
    Task UpsertAsync(
        Product product,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default);
}