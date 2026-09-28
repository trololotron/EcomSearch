using Confluent.Kafka;
using Microsoft.Extensions.Logging.Abstractions;
using Search.Application.Events;
using Search.Infrastructure.Messaging;
using System.Text.Json;

namespace Search.Application.Tests.Integration;

public sealed class KafkaEventPublisherTests
{
    [Fact]
    public async Task PublishAsync_PublishesProductUpdatedEvent()
    {
        // Arrange
        var fixture = new KafkaFixture();
        var topic = await fixture.CreateTopicAsync();

        try
        {
            using var runner = new KafkaConsumerRunner(
              topic,
              $"ecomsearch-test-{Guid.NewGuid():N}");

            await runner.WaitForAssignmentAsync(
                TimeSpan.FromSeconds(10));

            using var producer =
                new ProducerBuilder<string, string>(
                    new ProducerConfig
                    {
                        BootstrapServers =
                            KafkaFixture.BootstrapServers
                    })
                .Build();

            var publisher = new KafkaEventPublisher(
                producer,
                NullLogger<KafkaEventPublisher>.Instance);

            var message = new ProductUpdated(
                Guid.NewGuid(),
                "Test Product",
                "Test description",
                12345,
                "Test category",
                DateTimeOffset.UtcNow);

            // Act
            await publisher.PublishAsync(
                message,
                topic,
                message.ProductId.ToString());

            var consumeResult = await runner.ConsumeAsync(TimeSpan.FromSeconds(10));

            // Assert
            Assert.NotNull(consumeResult);

            Assert.Equal(
                message.ProductId.ToString(),
                consumeResult.Message.Key);

            var receivedMessage =
                JsonSerializer.Deserialize<ProductUpdated>(
                    consumeResult.Message.Value);

            Assert.NotNull(receivedMessage);

            Assert.Equal(
                message.ProductId,
                receivedMessage.ProductId);

            Assert.Equal(
                message.Name,
                receivedMessage.Name);

            Assert.Equal(
                message.Description,
                receivedMessage.Description);

            Assert.Equal(
                message.Price,
                receivedMessage.Price);

            Assert.Equal(
                message.Category,
                receivedMessage.Category);
        }
        finally
        {
            await fixture.DeleteTopicAsync(topic);
        }
    }

    [Fact]
    public async Task PublishAsync_WithSameKey_PublishesMessagesToSamePartition()
    {
        // Arrange
        var fixture = new KafkaFixture();
        var topic = await fixture.CreateTopicAsync(partitions: 3);

        try
        {
            using var runner = new KafkaConsumerRunner(topic, $"ecomsearch-test-{Guid.NewGuid():N}");

            var assignment =
                await runner.WaitForAssignmentAsync(
                    TimeSpan.FromSeconds(10));

            Assert.NotEmpty(assignment);

            using var producer =
                new ProducerBuilder<string, string>(
                    new ProducerConfig
                    {
                        BootstrapServers =
                            KafkaFixture.BootstrapServers
                    })
                .Build();

            var publisher = new KafkaEventPublisher(
                producer,
                NullLogger<KafkaEventPublisher>.Instance);

            var productId = Guid.NewGuid();
            var key = productId.ToString();

            var firstMessage = new ProductUpdated(
                productId,
                "Product version 1",
                "Description 1",
                1000,
                "Category",
                DateTimeOffset.UtcNow);

            var secondMessage = new ProductUpdated(
                productId,
                "Product version 2",
                "Description 2",
                2000,
                "Category",
                DateTimeOffset.UtcNow);

            // Act
            await publisher.PublishAsync(
                firstMessage,
                topic,
                key);

            await publisher.PublishAsync(
                secondMessage,
                topic,
                key);

            var firstResult = await runner.ConsumeAsync(
                TimeSpan.FromSeconds(10));

            var secondResult = await runner.ConsumeAsync(
                TimeSpan.FromSeconds(10));

            // Assert
            Assert.NotNull(firstResult);
            Assert.NotNull(secondResult);

            Assert.Equal(
                firstResult.Partition,
                secondResult.Partition);

            Assert.Equal(
                key,
                firstResult.Message.Key);

            Assert.Equal(
                key,
                secondResult.Message.Key);
        }
        finally
        {
            await fixture.DeleteTopicAsync(topic);
        }
    }

    [Fact]
    public async Task ConsumersInSameGroup_SharePartitions()
    {
        // Arrange
        var fixture = new KafkaFixture();
        var topic = await fixture.CreateTopicAsync(partitions: 3);
        var groupId = $"ecomsearch-test-group-{Guid.NewGuid():N}";

        try
        {
            using var firstRunner =
                new KafkaConsumerRunner(topic, groupId);

            using var secondRunner =
                new KafkaConsumerRunner(topic, groupId);

            var firstAssignment =
                await firstRunner.WaitForAssignmentAsync(
                    TimeSpan.FromSeconds(10));

            var secondAssignment =
                await secondRunner.WaitForAssignmentAsync(
                    TimeSpan.FromSeconds(10));

            Assert.NotEmpty(firstAssignment);
            Assert.NotEmpty(secondAssignment);

            Assert.Equal(
                3,
                firstAssignment.Count + secondAssignment.Count);

            Assert.Empty(
                firstAssignment.Intersect(secondAssignment));
        }
        finally
        {
            await fixture.DeleteTopicAsync(topic);
        }
    }
}