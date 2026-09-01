using Search.Domain;

namespace Search.Application.Abstractions;

public interface IProductIndexWriter
{
    Task UpsertAsync(
        Product product,
        CancellationToken cancellationToken = default);
}