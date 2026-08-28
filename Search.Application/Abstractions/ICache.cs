namespace Search.Application.Abstractions;

public interface ICache<T>
{
    Task<T?> GetAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        string key,
        T value,
        CancellationToken cancellationToken = default);
}