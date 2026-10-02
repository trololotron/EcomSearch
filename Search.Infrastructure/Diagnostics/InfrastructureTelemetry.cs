using System.Diagnostics;

namespace Search.Infrastructure.Diagnostics;

public static class InfrastructureTelemetry
{
    public const string ActivitySourceName =
        "EcomSearch.Infrastructure";

    public static readonly ActivitySource ActivitySource =
        new(ActivitySourceName);
}