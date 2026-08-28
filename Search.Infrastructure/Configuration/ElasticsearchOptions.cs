namespace Search.Infrastructure.Configuration;

public sealed class ElasticsearchOptions
{
    public string Url { get; init; } = string.Empty;

    public string ProductIndex { get; init; } = string.Empty;
}