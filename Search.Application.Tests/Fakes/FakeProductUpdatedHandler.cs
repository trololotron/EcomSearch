using Search.Application.Events;

namespace Search.Application.Tests.Fakes;

public sealed class FakeProductUpdatedHandler : IProductUpdatedHandler
{
    public List<ProductUpdated> Messages { get; } = [];

    public Task HandleAsync(
        ProductUpdated message,
        CancellationToken cancellationToken = default)
    {
        Messages.Add(message);

        return Task.CompletedTask;
    }
}