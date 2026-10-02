using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProductCatalog.Grpc.Services;
using Search.Application.Products;
using Search.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService("ProductCatalog.Grpc");
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();

        tracing.AddSource(
            DiagnosticsActivityEventSubscriber.ActivitySourceName);

        tracing.AddOtlpExporter(options =>
        {
            options.Endpoint =
                new Uri("http://localhost:4317");

            options.Protocol =
                OtlpExportProtocol.Grpc;
        });
    });

builder.Services.AddGrpc();
builder.Services.AddMongoPersistence(builder.Configuration);
builder.Services.AddScoped<GetProductById>();

var app = builder.Build();

app.MapGrpcService<ProductCatalogService>();

app.Run();
public partial class Program
{
}