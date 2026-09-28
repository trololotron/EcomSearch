using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Search.Application.Tests.Integration;

public sealed class KafkaFixture
{
    public const string BootstrapServers = "localhost:9092";

    public async Task<string> CreateTopicAsync(
        int partitions = 3,
        short replicationFactor = 1)
    {
        var topicName = $"ecomsearch-test-{Guid.NewGuid():N}";

        var config = new AdminClientConfig
        {
            BootstrapServers = BootstrapServers
        };

        using var adminClient =
            new AdminClientBuilder(config).Build();

        await adminClient.CreateTopicsAsync(
        [
            new TopicSpecification
            {
                Name = topicName,
                NumPartitions = partitions,
                ReplicationFactor = replicationFactor
            }
        ]);

        return topicName;
    }

    public async Task DeleteTopicAsync(string topic)
    {
        var config = new AdminClientConfig
        {
            BootstrapServers = BootstrapServers
        };

        using var adminClient =
            new AdminClientBuilder(config).Build();

        try
        {
            await adminClient.DeleteTopicsAsync([topic]);
        }
        catch (DeleteTopicsException)
        {
            // Topic may already have been deleted.
        }
    }
}