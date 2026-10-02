using Search.Application.Abstractions;
using Search.Domain;

namespace Search.Application.Products;

public sealed class GetProductById(
    IProductRepository productRepository)
{
    public Task<Product?> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return productRepository.GetByIdAsync(
            id,
            cancellationToken);
    }
}