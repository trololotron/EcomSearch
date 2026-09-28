using Search.Application.Abstractions;

namespace Search.Application.Tests.Fakes;

public sealed class FakeEventPublisher : IEventPublisher
{
    public object? Message { get; private set; }
    public string? Topic { get; private set; }
    public string? Key { get; private set; }
    public bool ShouldFail { get; set; }
    public Task PublishAsync<T>(
        T message,
        string topic,
        string key,
        CancellationToken cancellationToken = default)
    {
        if (ShouldFail)
        {
            throw new InvalidOperationException(
                "Kafka publish failed.");
        }

        Message = message;
        Topic = topic;
        Key = key;

        return Task.CompletedTask;
    }
}