using Search.Application.Events;

namespace Search.Application.Tests.Fakes;

public sealed class FailingProductUpdatedHandler : IProductUpdatedHandler
{
    private readonly Guid _productId;

    public int Attempts { get; private set; }

    public FailingProductUpdatedHandler(Guid productId)
    {
        _productId = productId;
    }

    public Task HandleAsync(
        ProductUpdated message,
        CancellationToken cancellationToken = default)
    {
        if (message.ProductId == _productId)
        {
            Attempts++;

            throw new InvalidOperationException(
                "Simulated handler failure.");
        }

        return Task.CompletedTask;
    }
}