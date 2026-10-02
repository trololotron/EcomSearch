using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Search.Application.Abstractions;
using Search.Application.Events;
using Search.Domain;
using Search.Infrastructure.Configuration;
using Search.Infrastructure.Diagnostics;
using System.Diagnostics;
using System.Text.Json;

namespace Search.Infrastructure.Persistence;

public sealed class MongoProductUpdateStore
    : IProductUpdateStore
{
    private readonly IMongoClient _client;
    private readonly IMongoCollection<Product> _products;
    private readonly IMongoCollection<OutboxMessage> _outbox;

    public MongoProductUpdateStore(
        IMongoClient client,
        IOptions<MongoOptions> options)
    {
        _client = client;

        var database = client.GetDatabase(
            options.Value.DatabaseName);

        _products = database.GetCollection<Product>(
            options.Value.ProductsCollection);

        _outbox = database.GetCollection<OutboxMessage>(
            options.Value.OutboxCollection);
    }

    public async Task SaveAsync(
        Product product,
        ProductUpdated productUpdated,
        CancellationToken cancellationToken = default)
    {
        using var activity =
            InfrastructureTelemetry.ActivitySource.StartActivity(
                "MongoProductUpdateStore.Save");

        activity?.SetTag("product.id", product.Id);

        using var session = await _client.StartSessionAsync(
            cancellationToken: cancellationToken);

        var currentActivity = Activity.Current;

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(ProductUpdated),
            Payload = JsonSerializer.Serialize(productUpdated),
            CreatedAt = DateTimeOffset.UtcNow,
            TraceParent = currentActivity?.Id,
            TraceState = currentActivity?.TraceStateString
        };

        await session.WithTransactionAsync(
            async (sessionHandle, ct) =>
            {
                var filter = Builders<Product>.Filter.Eq(
                    x => x.Id,
                    product.Id);

                await _products.ReplaceOneAsync(
                    sessionHandle,
                    filter,
                    product,
                    new ReplaceOptions
                    {
                        IsUpsert = true
                    },
                    ct);

                await _outbox.InsertOneAsync(
                    sessionHandle,
                    outboxMessage,
                    cancellationToken: ct);

                return true;
            },
            cancellationToken: cancellationToken);
    }
}