using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Search.Application.Events;
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
}