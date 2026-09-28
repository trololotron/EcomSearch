namespace Search.Application.Abstractions;

public interface IProductSearchCacheVersion
{
    Task<long> GetAsync(
        CancellationToken cancellationToken = default);

    Task<long> IncrementAsync(
        CancellationToken cancellationToken = default);
}