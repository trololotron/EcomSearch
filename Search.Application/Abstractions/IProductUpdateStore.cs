using Search.Application.Events;
using Search.Domain;

namespace Search.Application.Abstractions;

public interface IProductUpdateStore
{
    Task SaveAsync(
        Product product,
        ProductUpdated productUpdated,
        CancellationToken cancellationToken = default);
}