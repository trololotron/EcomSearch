using Search.Domain;

namespace Search.Application.Products;

public sealed class ProductSearchResult
{
    public IReadOnlyList<Product> Items { get; init; } = [];

    public long Total { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }
}