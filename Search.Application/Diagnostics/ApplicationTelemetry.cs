using System.Diagnostics;

namespace Search.Application.Diagnostics;

public static class ApplicationTelemetry
{
    public const string ActivitySourceName = "EcomSearch.Application";

    public static readonly ActivitySource ActivitySource =
        new(ActivitySourceName);
}