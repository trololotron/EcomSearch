using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Search.Application.Abstractions;

namespace Search.Infrastructure.Messaging;

public sealed class KafkaEventPublisher : IEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(
        IProducer<string, string> producer,
        ILogger<KafkaEventPublisher> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task PublishAsync<T>(
        T message,
        string topic,
        string key,
        CancellationToken cancellationToken = default)
    {
        var value = JsonSerializer.Serialize(message);

        var result = await _producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = key,
                Value = value
            },
            cancellationToken);

        _logger.LogInformation(
            "Kafka message published. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, Key: {Key}",
            result.Topic,
            result.Partition.Value,
            result.Offset.Value,
            key);
    }
}