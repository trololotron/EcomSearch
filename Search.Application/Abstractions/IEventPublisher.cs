namespace Search.Application.Abstractions;
public interface IEventPublisher
{
    Task PublishAsync<T>(
        T message,
        string topic,
        string key,
        CancellationToken cancellationToken = default);
}