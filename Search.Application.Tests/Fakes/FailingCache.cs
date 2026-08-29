using Search.Application.Abstractions;

namespace Search.Application.Tests.Fakes;

public sealed class FailingCache<T> : ICache<T>
{
    private readonly Exception _exception;
    private readonly bool _failOnGet;
    private readonly bool _failOnSet;

    public FailingCache(
        Exception exception,
        bool failOnGet = false,
        bool failOnSet = false)
    {
        _exception = exception;
        _failOnGet = failOnGet;
        _failOnSet = failOnSet;
    }

    public Task<T?> GetAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        if (_failOnGet)
        {
            throw _exception;
        }

        return Task.FromResult<T?>(default);
    }

    public Task SetAsync(
        string key,
        T value,
        CancellationToken cancellationToken = default)
    {
        if (_failOnSet)
        {
            throw _exception;
        }

        return Task.CompletedTask;
    }
}