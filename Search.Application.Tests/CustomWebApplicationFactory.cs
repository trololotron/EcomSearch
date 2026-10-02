using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Search.Application.Abstractions;
using Search.Application.Tests.Fakes;
using Search.Infrastructure.Messaging;
using Search.Infrastructure.Outbox;

namespace Search.Application.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var outboxPublisher = services.FirstOrDefault(descriptor =>
                        descriptor.ServiceType == typeof(IHostedService) &&
                        descriptor.ImplementationType == typeof(OutboxPublisher));

            if (outboxPublisher is not null)
            {
                services.Remove(outboxPublisher);
            }

            var outboxMetricsCollector = services.FirstOrDefault(
                        descriptor =>
                        descriptor.ServiceType == typeof(IHostedService) &&
                        descriptor.ImplementationType == typeof(OutboxMetricsCollector));

            if (outboxMetricsCollector is not null)
            {
                services.Remove(outboxMetricsCollector);
            }

            services.RemoveAll<IProductSearchRepository>();

            services.AddScoped<
                IProductSearchRepository,
                FakeProductSearchRepository>();

            var kafkaConsumer = services.FirstOrDefault(
                descriptor =>
                    descriptor.ServiceType == typeof(IHostedService) &&
                    descriptor.ImplementationType ==
                        typeof(KafkaProductUpdatedConsumer));

            if (kafkaConsumer is not null)
            {
                services.Remove(kafkaConsumer);
            }
        });
    }
}