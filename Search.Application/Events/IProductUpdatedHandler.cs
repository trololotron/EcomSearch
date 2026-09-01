namespace Search.Application.Events;

public interface IProductUpdatedHandler
{
    Task HandleAsync(
        ProductUpdated message,
        CancellationToken cancellationToken = default);
}