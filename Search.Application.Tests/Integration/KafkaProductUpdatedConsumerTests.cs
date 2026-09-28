using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Search.Application.Events;
using Search.Application.Tests.Fakes;
using Search.Infrastructure.Messaging;
using System.Text.Json;

namespace Search.Application.Tests.Integration;

public sealed class KafkaProductUpdatedConsumerTests
{
    private static ServiceProvider CreateServiceProvider(
    IProductUpdatedHandler handler)
    {
        var services = new ServiceCollection();

        services.AddScoped<IProductUpdatedHandler>(_ => handler);

        services.AddLogging();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Consumer_WhenProductUpdatedPublished_CallsHandler()
    {
        const string topic = "product-updated";

        var groupId = $"ecomsearch-test-{Guid.NewGuid():N}";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = KafkaFixture.BootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(
            consumerConfig).Build();

        var handler = new FakeProductUpdatedHandler();

        using var serviceProvider =
            CreateServiceProvider(handler);

        var scopeFactory =
            serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var loggerFactory =
            serviceProvider.GetRequiredService<ILoggerFactory>();

        var logger =
            loggerFactory.CreateLogger<KafkaProductUpdatedConsumer>();

        var service = new KafkaProductUpdatedConsumer(
            consumer,
            scopeFactory,
            logger);

        using var producer = new ProducerBuilder<string, string>(
            new ProducerConfig
            {
                BootstrapServers = KafkaFixture.BootstrapServers
            })
            .Build();

        var productId = Guid.NewGuid();

        var message = new ProductUpdated(
            productId,
            "iPhone 17",
            "Smartphone",
            999.99m,
            "Phones",
            DateTimeOffset.UtcNow);

        await service.StartAsync(CancellationToken.None);

        await producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = productId.ToString(),
                Value = JsonSerializer.Serialize(message)
            });

        var timeout = TimeSpan.FromSeconds(10);
        var startedAt = DateTime.UtcNow;

        while (!handler.Messages.Any(
                   x => x.ProductId == productId) &&
               DateTime.UtcNow - startedAt < timeout)
        {
            await Task.Delay(100);
        }

        await service.StopAsync(CancellationToken.None);
        loggerFactory.Dispose();

        Assert.Contains(
            handler.Messages,
            x => x.ProductId == productId);

        var handledMessage = handler.Messages.First(
            x => x.ProductId == productId);

        Assert.Equal(productId, handledMessage.ProductId);
        Assert.Equal("iPhone 17", handledMessage.Name);
        Assert.Equal("Smartphone", handledMessage.Description);
        Assert.Equal(999.99m, handledMessage.Price);
        Assert.Equal("Phones", handledMessage.Category);
    }

    [Fact]
    public async Task Consumer_AfterSuccessfulHandling_DoesNotReadMessageAgain()
    {
        const string topic = "product-updated";

        var groupId = $"ecomsearch-test-{Guid.NewGuid():N}";

        using var loggerFactory =
            LoggerFactory.Create(builder => { });

        using var producer = new ProducerBuilder<string, string>(
            new ProducerConfig
            {
                BootstrapServers = KafkaFixture.BootstrapServers
            })
            .Build();

        var productId = Guid.NewGuid();

        var message = new ProductUpdated(
            productId,
            "MacBook",
            "Laptop",
            1999m,
            "Laptops",
            DateTimeOffset.UtcNow);

        await producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = productId.ToString(),
                Value = JsonSerializer.Serialize(message)
            });

        var handler = new FakeProductUpdatedHandler();

        using var serviceProvider = CreateServiceProvider(handler);

        var scopeFactory =
            serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var logger =
            loggerFactory.CreateLogger<KafkaProductUpdatedConsumer>();

        using var consumer = new ConsumerBuilder<string, string>(
            new ConsumerConfig
            {
                BootstrapServers = KafkaFixture.BootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            })
            .Build();

        var service = new KafkaProductUpdatedConsumer(
            consumer,
            scopeFactory,
            logger);

        await service.StartAsync(CancellationToken.None);

        var timeout = TimeSpan.FromSeconds(10);
        var startedAt = DateTime.UtcNow;

        while (!handler.Messages.Any(
                   x => x.ProductId == productId) &&
               DateTime.UtcNow - startedAt < timeout)
        {
            await Task.Delay(100);
        }

        Assert.Contains(
            handler.Messages,
            x => x.ProductId == productId);

        await service.StopAsync(CancellationToken.None);

        using var secondConsumer =
            new ConsumerBuilder<string, string>(
                new ConsumerConfig
                {
                    BootstrapServers = KafkaFixture.BootstrapServers,
                    GroupId = groupId,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = false
                })
                .Build();

        secondConsumer.Subscribe(topic);

        var result = secondConsumer.Consume(
            TimeSpan.FromSeconds(2));

        Assert.True(
            result is null ||
            result.Message.Value == null ||
            JsonSerializer.Deserialize<ProductUpdated>(
                result.Message.Value)?.ProductId != productId);
    }

    [Fact]
    public async Task Consumer_WhenHandlerFails_DoesNotCommitMessage()
    {
        const string topic = "product-updated";

        var groupId = $"ecomsearch-test-{Guid.NewGuid():N}";

        using var loggerFactory =
            LoggerFactory.Create(builder => { });

        using var producer = new ProducerBuilder<string, string>(
            new ProducerConfig
            {
                BootstrapServers = KafkaFixture.BootstrapServers
            })
            .Build();

        var productId = Guid.NewGuid();

        var message = new ProductUpdated(
            productId,
            "Failure Test Product",
            "Test",
            100m,
            "Tests",
            DateTimeOffset.UtcNow);

        await producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = productId.ToString(),
                Value = JsonSerializer.Serialize(message)
            });

        var failingHandler =
            new FailingProductUpdatedHandler(productId);

        using var serviceProvider = CreateServiceProvider(failingHandler);

        var scopeFactory =
            serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var logger =
            loggerFactory.CreateLogger<KafkaProductUpdatedConsumer>();

        using var consumer = new ConsumerBuilder<string, string>(
            new ConsumerConfig
            {
                BootstrapServers = KafkaFixture.BootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            })
            .Build();

        var service = new KafkaProductUpdatedConsumer(
            consumer,
            scopeFactory,
            logger);

        await service.StartAsync(CancellationToken.None);

        var timeout = TimeSpan.FromSeconds(10);
        var startedAt = DateTime.UtcNow;

        while (failingHandler.Attempts == 0 &&
               DateTime.UtcNow - startedAt < timeout)
        {
            await Task.Delay(100);
        }

        Assert.True(failingHandler.Attempts > 0);

        await service.StopAsync(CancellationToken.None);

        var successfulHandler =
            new FakeProductUpdatedHandler();

        using var secondServiceProvider = CreateServiceProvider(successfulHandler);

        var secondScopeFactory =
            secondServiceProvider
                .GetRequiredService<IServiceScopeFactory>();

        var secondLoggerFactory =
            secondServiceProvider
                .GetRequiredService<ILoggerFactory>();

        var secondLogger =
            secondLoggerFactory
                .CreateLogger<KafkaProductUpdatedConsumer>();

        using var secondConsumer =
            new ConsumerBuilder<string, string>(
                new ConsumerConfig
                {
                    BootstrapServers = KafkaFixture.BootstrapServers,
                    GroupId = groupId,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = false
                })
                .Build();

        var secondService =
            new KafkaProductUpdatedConsumer(
                secondConsumer,
                secondScopeFactory,
                secondLogger);

        await secondService.StartAsync(CancellationToken.None);

        var secondStartedAt = DateTime.UtcNow;

        while (!successfulHandler.Messages.Any(
                   x => x.ProductId == productId) &&
               DateTime.UtcNow - secondStartedAt < timeout)
        {
            await Task.Delay(100);
        }

        await secondService.StopAsync(CancellationToken.None);

        Assert.Contains(
            successfulHandler.Messages,
            x => x.ProductId == productId);
    }
}