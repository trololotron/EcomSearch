using System.Diagnostics.Metrics;

namespace Search.Infrastructure.Diagnostics;

public static class InfrastructureMetrics
{
    public const string MeterName = "EcomSearch.Infrastructure";

    public static readonly Meter Meter =
        new(MeterName);

    public static readonly Counter<long> OutboxProcessed =
        Meter.CreateCounter<long>(
            "ecomsearch.outbox.processed",
            description: "Number of successfully processed outbox messages");

    public static readonly Counter<long> KafkaConsumed =
        Meter.CreateCounter<long>(
            "ecomsearch.kafka.consumed",
            description: "Number of successfully processed Kafka messages");

    public static readonly Gauge<long> OutboxPending =
    Meter.CreateGauge<long>(
        "ecomsearch.outbox.pending",
        description: "Current number of unprocessed outbox messages");
}