namespace Search.Infrastructure.Configuration;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; init; } = string.Empty;
}