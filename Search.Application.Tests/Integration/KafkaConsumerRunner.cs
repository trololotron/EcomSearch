using System.Threading.Channels;
using Confluent.Kafka;

namespace Search.Application.Tests.Integration;

public sealed class KafkaConsumerRunner : IDisposable
{
    private readonly IConsumer<string, string> _consumer;
    private readonly CancellationTokenSource _cts = new();

    private readonly Channel<ConsumeResult<string, string>> _messages =
        Channel.CreateUnbounded<ConsumeResult<string, string>>();

    private readonly TaskCompletionSource<bool> _assignmentReceived =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly Task _consumeTask;

    public KafkaConsumerRunner(
        string topic,
        string groupId)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = KafkaFixture.BootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config)
            .Build();

        _consumer.Subscribe(topic);

        _consumeTask = Task.Run(ConsumeLoopAsync);
    }

    public IReadOnlyCollection<TopicPartition> Assignment =>
        _consumer.Assignment;

    public async Task<IReadOnlyCollection<TopicPartition>>
        WaitForAssignmentAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
    {
        await _assignmentReceived.Task.WaitAsync(
            timeout,
            cancellationToken);

        return _consumer.Assignment;
    }

    public async Task<ConsumeResult<string, string>> ConsumeAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        return await _messages.Reader
            .ReadAsync(cancellationToken)
            .AsTask()
            .WaitAsync(timeout, cancellationToken);
    }

    private async Task ConsumeLoopAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var result = _consumer.Consume(
                    TimeSpan.FromMilliseconds(100));

                if (_consumer.Assignment.Count > 0)
                {
                    _assignmentReceived.TrySetResult(true);
                }

                if (result is not null)
                {
                    await _messages.Writer.WriteAsync(
                        result,
                        _cts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            _messages.Writer.TryComplete();
        }
    }

    public void Dispose()
    {
        _cts.Cancel();

        try
        {
            _consumeTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
        }

        _consumer.Close();
        _consumer.Dispose();
        _cts.Dispose();
    }
}