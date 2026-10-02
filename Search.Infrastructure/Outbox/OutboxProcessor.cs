using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Search.Application.Abstractions;
using Search.Application.Events;
using Search.Infrastructure.Configuration;
using Search.Infrastructure.Diagnostics;
using Search.Infrastructure.Persistence;
using System.Diagnostics;
using System.Text.Json;

namespace Search.Infrastructure.Outbox;

public sealed class OutboxProcessor
{
    private const string ProductUpdatedTopic = "product-updated";

    private readonly IMongoCollection<OutboxMessage> _outbox;
    private readonly IEventPublisher _eventPublisher;

    public OutboxProcessor(
        IMongoClient mongoClient,
        IOptions<MongoOptions> options,
        IEventPublisher eventPublisher)
    {
        var database = mongoClient.GetDatabase(
            options.Value.DatabaseName);

        _outbox = database.GetCollection<OutboxMessage>(
            options.Value.OutboxCollection);

        _eventPublisher = eventPublisher;
    }

    public async Task<bool> ProcessNextAsync(
        CancellationToken cancellationToken = default)
    {
        var message = await _outbox
            .Find(x => x.ProcessedAt == null)
            .SortBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (message is null)
        {
            return false;
        }

        using var activity = StartOutboxActivity(message);

        activity?.SetTag(
            "outbox.message.id",
            message.Id);

        activity?.SetTag(
            "outbox.message.type",
            message.Type);

        if (message.Type != nameof(ProductUpdated))
        {
            throw new InvalidOperationException(
                $"Unknown outbox message type: {message.Type}");
        }

        var productUpdated =
            JsonSerializer.Deserialize<ProductUpdated>(
                message.Payload);

        if (productUpdated is null)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize outbox message {message.Id}.");
        }

        await _eventPublisher.PublishAsync(
            productUpdated,
            ProductUpdatedTopic,
            productUpdated.ProductId.ToString(),
            cancellationToken);

        var update = Builders<OutboxMessage>
            .Update
            .Set(
                x => x.ProcessedAt,
                DateTimeOffset.UtcNow);

        await _outbox.UpdateOneAsync(
            x => x.Id == message.Id &&
                 x.ProcessedAt == null,
            update,
            cancellationToken: cancellationToken);

        InfrastructureMetrics.OutboxProcessed.Add(1);

        return true;
    }

    private static Activity? StartOutboxActivity(
    OutboxMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.TraceParent) &&
            ActivityContext.TryParse(
                message.TraceParent,
                message.TraceState,
                isRemote: true,
                out var parentContext))
        {
            return InfrastructureTelemetry.ActivitySource.StartActivity(
                "Outbox.Process",
                ActivityKind.Internal,
                parentContext);
        }

        return InfrastructureTelemetry.ActivitySource.StartActivity(
            "Outbox.Process",
            ActivityKind.Internal);
    }

    public Task<long> GetPendingCountAsync(
    CancellationToken cancellationToken = default)
    {
        return _outbox.CountDocumentsAsync(
            x => x.ProcessedAt == null,
            cancellationToken: cancellationToken);
    }

}