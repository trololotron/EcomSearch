using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Search.Application.Abstractions;
using Search.Infrastructure.Diagnostics;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

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
        using var activity =
        InfrastructureTelemetry.ActivitySource.StartActivity(
            "Kafka.Publish",
            ActivityKind.Producer);

        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination.name", topic);
        activity?.SetTag("messaging.kafka.message.key", key);

        var headers = new Headers();

        var currentActivity = Activity.Current;

        if (currentActivity?.Id is not null)
        {
            headers.Add(
                "traceparent",
                Encoding.UTF8.GetBytes(currentActivity.Id));

            if (!string.IsNullOrWhiteSpace(
                    currentActivity.TraceStateString))
            {
                headers.Add(
                    "tracestate",
                    Encoding.UTF8.GetBytes(
                        currentActivity.TraceStateString));
            }
        }

        var value = JsonSerializer.Serialize(message);

        var result = await _producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = key,
                Value = JsonSerializer.Serialize(message),
                Headers = headers
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