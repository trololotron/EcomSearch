using System.ComponentModel.DataAnnotations;

namespace Search.Application.Products;

public sealed class ProductSearchRequest
{
    public string Query { get; init; } = string.Empty;

    public string? Category { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    public ProductSort Sort { get; init; } = ProductSort.Relevance;
}