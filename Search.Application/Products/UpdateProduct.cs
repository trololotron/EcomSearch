using Search.Application.Abstractions;
using Search.Application.Diagnostics;
using Search.Application.Events;
using Search.Domain;

public sealed class UpdateProduct
{
    private readonly IProductUpdateStore _store;

    public UpdateProduct(
        IProductUpdateStore store)
    {
        _store = store;
    }

    public async Task ExecuteAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        using var activity =
            ApplicationTelemetry.ActivitySource.StartActivity(
                "UpdateProduct");
        activity?.SetTag("product.id", product.Id);

        var message = new ProductUpdated(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Category,
            DateTimeOffset.UtcNow);

        await _store.SaveAsync(
            product,
            message,
            cancellationToken);
    }
}