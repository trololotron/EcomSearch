using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Search.Application.Events;
using Search.Infrastructure.Diagnostics;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Search.Infrastructure.Messaging;

public sealed class KafkaProductUpdatedConsumer
    : BackgroundService
{
    private const string Topic = "product-updated";

    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaProductUpdatedConsumer> _logger;

    public KafkaProductUpdatedConsumer(
        IConsumer<string, string> consumer,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaProductUpdatedConsumer> logger)
    {
        _consumer = consumer;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _consumer.Subscribe(Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = _consumer.Consume(
                    stoppingToken);

                using var activity = StartConsumerActivity(result);

                activity?.SetTag(
                    "messaging.system",
                    "kafka");

                activity?.SetTag(
                    "messaging.destination.name",
                    result.Topic);

                activity?.SetTag(
                    "messaging.kafka.partition",
                    result.Partition.Value);

                activity?.SetTag(
                    "messaging.kafka.offset",
                    result.Offset.Value);

                activity?.SetTag(
                    "messaging.kafka.message.key",
                    result.Message.Key);

                var message =
                    JsonSerializer.Deserialize<ProductUpdated>(
                        result.Message.Value);

                if (message is null)
                {
                    _logger.LogWarning(
                        "Failed to deserialize ProductUpdated message.");

                    continue;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var handler = scope.ServiceProvider
                        .GetRequiredService<IProductUpdatedHandler>();

                    await handler.HandleAsync(
                        message,
                        stoppingToken);

                    _consumer.Commit(result);

                    InfrastructureMetrics.KafkaConsumed.Add(1);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to handle ProductUpdated event for product {ProductId}. Message will not be committed.",
                        message.ProductId);
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            _consumer.Close();
        }
    }

    private static Activity? StartConsumerActivity(
    ConsumeResult<string, string> result)
    {
        var headers = result.Message.Headers;

        if (headers is not null &&
            headers.TryGetLastBytes(
                "traceparent",
                out var traceParentBytes))
        {
            var traceParent =
                Encoding.UTF8.GetString(
                    traceParentBytes);

            string? traceState = null;

            if (headers.TryGetLastBytes(
                    "tracestate",
                    out var traceStateBytes))
            {
                traceState =
                    Encoding.UTF8.GetString(
                        traceStateBytes);
            }

            if (ActivityContext.TryParse(
                    traceParent,
                    traceState,
                    isRemote: true,
                    out var parentContext))
            {
                return InfrastructureTelemetry
                    .ActivitySource
                    .StartActivity(
                        "Kafka.Consume",
                        ActivityKind.Consumer,
                        parentContext);
            }
        }

        return InfrastructureTelemetry
            .ActivitySource
            .StartActivity(
                "Kafka.Consume",
                ActivityKind.Consumer);
    }
}