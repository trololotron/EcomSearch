using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Search.Application.Events;
using Search.Application.Tests.Fakes;
using Search.Domain;
using Search.Infrastructure.Configuration;
using Search.Infrastructure.Outbox;
using Search.Infrastructure.Persistence;
using System.Text.Json;

namespace Search.Application.Tests.Intagration.Mongo;

public sealed class MongoProductUpdateStoreIntegrationTests
{
    public MongoProductUpdateStoreIntegrationTests()
    {
        MongoSerializationConfiguration.Configure();
    }

    [Fact]
    public async Task SaveAsync_SavesProductAndOutboxMessage()
    {
        var databaseName = $"ecomsearch-tests-{Guid.NewGuid():N}";

        var client = new MongoClient(
            "mongodb://localhost:27017/?replicaSet=rs0");

        var options = Options.Create(new MongoOptions
        {
            ConnectionString =
                "mongodb://localhost:27017/?replicaSet=rs0",
            DatabaseName = databaseName,
            ProductsCollection = "products",
            OutboxCollection = "outbox"
        });

        var store = new MongoProductUpdateStore(
            client,
            options);

        var productId = Guid.NewGuid();

        var product = new Product
        {
            Id = productId,
            Name = "iPhone 17",
            Description = "Smartphone",
            Price = 999.99m,
            Category = "Phones"
        };

        var productUpdated = new ProductUpdated(
            productId,
            product.Name,
            product.Description,
            product.Price,
            product.Category,
            DateTimeOffset.UtcNow);

        try
        {
            await store.SaveAsync(
                product,
                productUpdated);

            var database = client.GetDatabase(databaseName);

            var products =
                database.GetCollection<Product>("products");

            var outbox =
                database.GetCollection<OutboxMessage>("outbox");

            var savedProduct = await products
                .Find(x => x.Id == productId)
                .SingleAsync();

            var savedOutboxMessage = await outbox
                .Find(x => x.Type == nameof(ProductUpdated))
                .SingleAsync();

            Assert.Equal(productId, savedProduct.Id);
            Assert.Equal("iPhone 17", savedProduct.Name);

            Assert.Null(savedOutboxMessage.ProcessedAt);

            var savedEvent =
                JsonSerializer.Deserialize<ProductUpdated>(
                    savedOutboxMessage.Payload);

            Assert.NotNull(savedEvent);
            Assert.Equal(productId, savedEvent.ProductId);
            Assert.Equal("iPhone 17", savedEvent.Name);
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }
    [Fact]
    public async Task SaveAsync_WhenOutboxInsertFails_RollsBackProduct()
    {
        var databaseName = $"ecomsearch-tests-{Guid.NewGuid():N}";

        var client = new MongoClient(
            "mongodb://localhost:27017/?replicaSet=rs0");

        var options = Options.Create(new MongoOptions
        {
            ConnectionString =
                "mongodb://localhost:27017/?replicaSet=rs0",
            DatabaseName = databaseName,
            ProductsCollection = "products",
            OutboxCollection = "outbox"
        });

        var database = client.GetDatabase(databaseName);

        var products =
            database.GetCollection<Product>("products");

        var outbox =
            database.GetCollection<OutboxMessage>("outbox");

        try
        {
            var indexKeys =
                Builders<OutboxMessage>
                    .IndexKeys
                    .Ascending(x => x.Type);

            await outbox.Indexes.CreateOneAsync(
                new CreateIndexModel<OutboxMessage>(
                    indexKeys,
                    new CreateIndexOptions
                    {
                        Unique = true
                    }));

            await outbox.InsertOneAsync(
                new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = nameof(ProductUpdated),
                    Payload = "{}",
                    CreatedAt = DateTimeOffset.UtcNow
                });

            var store = new MongoProductUpdateStore(
                client,
                options);

            var productId = Guid.NewGuid();

            var product = new Product
            {
                Id = productId,
                Name = "iPhone 17",
                Description = "Smartphone",
                Price = 999.99m,
                Category = "Phones"
            };

            var productUpdated = new ProductUpdated(
                productId,
                product.Name,
                product.Description,
                product.Price,
                product.Category,
                DateTimeOffset.UtcNow);

            await Assert.ThrowsAnyAsync<MongoException>(
                () => store.SaveAsync(
                    product,
                    productUpdated));

            var savedProduct = await products
                .Find(x => x.Id == productId)
                .FirstOrDefaultAsync();

            Assert.Null(savedProduct);

            var outboxCount = await outbox.CountDocumentsAsync(
                x => x.Type == nameof(ProductUpdated));

            Assert.Equal(1, outboxCount);
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task ProcessNextAsync_WhenPublishSucceeds_MarksMessageAsProcessed()
    {
        MongoSerializationConfiguration.Configure();

        var databaseName =
            $"ecomsearch-tests-{Guid.NewGuid():N}";

        var client = new MongoClient(
            "mongodb://localhost:27017/?replicaSet=rs0");

        var options = Options.Create(new MongoOptions
        {
            ConnectionString =
                "mongodb://localhost:27017/?replicaSet=rs0",
            DatabaseName = databaseName,
            ProductsCollection = "products",
            OutboxCollection = "outbox"
        });

        var publisher = new FakeEventPublisher();

        try
        {
            var database = client.GetDatabase(databaseName);

            var outbox =
                database.GetCollection<OutboxMessage>(
                    "outbox");

            var productId = Guid.NewGuid();

            var productUpdated = new ProductUpdated(
                productId,
                "iPhone 17",
                "Smartphone",
                999.99m,
                "Phones",
                DateTimeOffset.UtcNow);

            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(ProductUpdated),
                Payload = JsonSerializer.Serialize(
                    productUpdated),
                CreatedAt = DateTimeOffset.UtcNow
            };

            await outbox.InsertOneAsync(message);

            var processor = new OutboxProcessor(
                client,
                options,
                publisher);

            var processed =
                await processor.ProcessNextAsync();

            Assert.True(processed);

            var saved = await outbox
                .Find(x => x.Id == message.Id)
                .SingleAsync();

            Assert.NotNull(saved.ProcessedAt);

            var published =
                Assert.IsType<ProductUpdated>(
                    publisher.Message);

            Assert.Equal(productId, published.ProductId);
            Assert.Equal("product-updated", publisher.Topic);
            Assert.Equal(productId.ToString(), publisher.Key);
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task ProcessNextAsync_WhenPublishFails_AsProcessedAt_is_Null()
    {
        MongoSerializationConfiguration.Configure();

        var databaseName =
            $"ecomsearch-tests-{Guid.NewGuid():N}";

        var client = new MongoClient(
            "mongodb://localhost:27017/?replicaSet=rs0");

        var options = Options.Create(new MongoOptions
        {
            ConnectionString =
                "mongodb://localhost:27017/?replicaSet=rs0",
            DatabaseName = databaseName,
            ProductsCollection = "products",
            OutboxCollection = "outbox"
        });

        var publisher = new FakeEventPublisher
        {
            ShouldFail = true
        };

        try
        {
            var database = client.GetDatabase(databaseName);

            var outbox =
                database.GetCollection<OutboxMessage>(
                    "outbox");

            var productId = Guid.NewGuid();

            var productUpdated = new ProductUpdated(
                productId,
                "iPhone 17",
                "Smartphone",
                999.99m,
                "Phones",
                DateTimeOffset.UtcNow);

            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(ProductUpdated),
                Payload = JsonSerializer.Serialize(
                    productUpdated),
                CreatedAt = DateTimeOffset.UtcNow
            };

            await outbox.InsertOneAsync(message);

            var processor = new OutboxProcessor(
                client,
                options,
                publisher);

            /*      var exception =
                      await Assert.ThrowsAsync<InvalidOperationException>(
                      () => processor.ProcessNextAsync());

                  Assert.Equal(
                      "Kafka publish failed.",
                      exception.Message);*/

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => processor.ProcessNextAsync());

            var saved = await outbox
                .Find(x => x.Id == message.Id)
                .SingleAsync();

            Assert.Null(saved.ProcessedAt);

        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }

    }
}