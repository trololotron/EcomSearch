namespace Search.Infrastructure.Configuration;

public sealed class MongoOptions
{
    public string ConnectionString { get; init; } = string.Empty;

    public string DatabaseName { get; init; } = string.Empty;

    public string ProductsCollection { get; init; } = string.Empty;

    public string OutboxCollection { get; init; } = string.Empty;
}