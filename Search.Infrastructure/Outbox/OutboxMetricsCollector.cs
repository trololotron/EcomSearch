using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Search.Infrastructure.Diagnostics;

namespace Search.Infrastructure.Outbox;

public sealed class OutboxMetricsCollector : BackgroundService
{
    private readonly OutboxProcessor _processor;
    private readonly ILogger<OutboxMetricsCollector> _logger;

    public OutboxMetricsCollector(
        OutboxProcessor processor,
        ILogger<OutboxMetricsCollector> logger)
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
                var pending =
                    await _processor.GetPendingCountAsync(
                        stoppingToken);

                InfrastructureMetrics.OutboxPending.Record(
                    pending);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to collect outbox pending metric.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(1),
                stoppingToken);
        }
    }
}