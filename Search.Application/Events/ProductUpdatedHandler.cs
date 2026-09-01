using Search.Application.Abstractions;
using Search.Domain;

namespace Search.Application.Events;

public sealed class ProductUpdatedHandler
    : IProductUpdatedHandler
{
    private readonly IProductIndexWriter _indexWriter;

    public ProductUpdatedHandler(
        IProductIndexWriter indexWriter)
    {
        _indexWriter = indexWriter;
    }

    public Task HandleAsync(
        ProductUpdated message,
        CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Id = message.ProductId,
            Name = message.Name,
            Description = message.Description,
            Price = message.Price,
            Category = message.Category
        };

        return _indexWriter.UpsertAsync(
            product,
            cancellationToken);
    }
}