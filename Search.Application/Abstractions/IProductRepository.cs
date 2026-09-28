using Search.Domain;

namespace Search.Application.Abstractions;

public interface IProductRepository
{
    Task UpsertAsync(
        Product product,
        CancellationToken cancellationToken = default);
}