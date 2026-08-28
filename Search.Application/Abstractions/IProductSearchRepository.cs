using Search.Application.Products;
using Search.Domain;

namespace Search.Application.Abstractions;

public interface IProductSearchRepository
{
    Task<ProductSearchResult> SearchAsync(
        ProductSearchRequest request,
        CancellationToken cancellationToken = default);
}