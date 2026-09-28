using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Search.Infrastructure.Outbox;

public sealed class OutboxPublisher : BackgroundService
{
    private readonly OutboxProcessor _processor;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(
        OutboxProcessor processor,
        ILogger<OutboxPublisher> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed =
                    await _processor.ProcessNextAsync(
                        stoppingToken);

                if (!processed)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(1),
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process outbox message.");

                await Task.Delay(
                    TimeSpan.FromSeconds(2),
                    stoppingToken);
            }
        }
    }
}