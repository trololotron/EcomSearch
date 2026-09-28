using Search.Application.Abstractions;

namespace Search.Application.Tests.Fakes;

public sealed class FakeProductSearchCacheVersion
    : IProductSearchCacheVersion
{
    public long Version { get; set; }

    public bool FailOnGet { get; set; }

    public bool FailOnIncrement { get; set; }

    public Task<long> GetAsync(
        CancellationToken cancellationToken = default)
    {
        if (FailOnGet)
        {
            throw new InvalidOperationException(
                "Redis cache version GET failed.");
        }

        return Task.FromResult(Version);
    }

    public Task<long> IncrementAsync(
        CancellationToken cancellationToken = default)
    {
        if (FailOnIncrement)
        {
            throw new InvalidOperationException(
                "Redis cache version INCREMENT failed.");
        }

        Version++;

        return Task.FromResult(Version);
    }
}