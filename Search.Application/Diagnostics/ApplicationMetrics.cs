using System.Diagnostics.Metrics;

namespace Search.Application.Diagnostics;

public static class ApplicationMetrics
{
    public const string MeterName = "EcomSearch.Application";

    public static readonly Meter Meter =
        new(MeterName);

    public static readonly Counter<long> CacheHits =
        Meter.CreateCounter<long>(
            "ecomsearch.cache.hits",
            description: "Number of product search cache hits");

    public static readonly Counter<long> CacheMisses =
        Meter.CreateCounter<long>(
            "ecomsearch.cache.misses",
            description: "Number of product search cache misses");
}