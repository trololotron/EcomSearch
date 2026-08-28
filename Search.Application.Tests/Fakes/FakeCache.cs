using Search.Application.Abstractions;

namespace Search.Application.Tests.Fakes;

public sealed class FakeCache<T> : ICache<T>
{
    private readonly Dictionary<string, T> _storage = new();

    public int GetCallCount { get; private set; }

    public int SetCallCount { get; private set; }

    public Task<T?> GetAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        GetCallCount++;

        _storage.TryGetValue(key, out var value);

        return Task.FromResult<T?>(value);
    }

    public Task SetAsync(
        string key,
        T value,
        CancellationToken cancellationToken = default)
    {
        SetCallCount++;

        _storage[key] = value;

        return Task.CompletedTask;
    }
}