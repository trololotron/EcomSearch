using Search.Application.Abstractions;
using Search.Application.Products;
using Search.Domain;

namespace Search.Application.Tests.Fakes;

public sealed class FakeProductSearchRepository : IProductSearchRepository
{
    public string? LastQuery { get; private set; }

    public CancellationToken LastCancellationToken { get; private set; }

    public int SearchCallCount { get; private set; }

    public Task<ProductSearchResult> SearchAsync(
        ProductSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        SearchCallCount++;

        LastQuery = request.Query;
        LastCancellationToken = cancellationToken;

        if (!request.Query.Equals(
                "iphone",
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                new ProductSearchResult
                {
                    Items = [],
                    Total = 0,
                    Page = request.Page,
                    PageSize = request.PageSize
                });
        }

        IReadOnlyList<Product> products =
        [
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "iPhone 17 Pro",
                Description = "Флагманский смартфон Apple",
                Price = 129999,
                Category = "Смартфоны"
            }
        ];

        return Task.FromResult(
            new ProductSearchResult
            {
                Items = products,
                Total = 1,
                Page = request.Page,
                PageSize = request.PageSize
            });
    }
}