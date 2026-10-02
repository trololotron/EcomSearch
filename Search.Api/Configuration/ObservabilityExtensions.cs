using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Search.Application.Diagnostics;
using Search.Infrastructure.Diagnostics;

namespace Search.Api.Configuration;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(
        this WebApplicationBuilder builder)
    {
        builder.Logging.Configure(options =>
        {
            options.ActivityTrackingOptions =
                ActivityTrackingOptions.TraceId |
                ActivityTrackingOptions.SpanId;
        });

        builder.Logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
        });

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService("EcomSearch.Api"))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddGrpcClientInstrumentation();

                tracing.AddSource(
                    ApplicationTelemetry.ActivitySourceName);

                tracing.AddSource(
                    InfrastructureTelemetry.ActivitySourceName);

                if (builder.Environment.IsDevelopment())
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint =
                            new Uri("http://localhost:4317");

                        options.Protocol =
                            OtlpExportProtocol.Grpc;
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();

                metrics.AddMeter(
                    ApplicationMetrics.MeterName);

                metrics.AddMeter(
                    InfrastructureMetrics.MeterName);

                if (builder.Environment.IsDevelopment())
                {
                    metrics.AddOtlpExporter(
                        (exporterOptions, readerOptions) =>
                        {
                            exporterOptions.Endpoint =
                                new Uri(
                                    "http://localhost:9090/api/v1/otlp/v1/metrics");

                            exporterOptions.Protocol =
                                OtlpExportProtocol.HttpProtobuf;

                            readerOptions
                                .PeriodicExportingMetricReaderOptions
                                .ExportIntervalMilliseconds = 5000;
                        });
                }
            });

        return builder;
    }
}