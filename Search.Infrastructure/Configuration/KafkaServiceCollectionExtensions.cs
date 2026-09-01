using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Search.Application.Abstractions;
using Search.Application.Events;
using Search.Infrastructure.Messaging;

namespace Search.Infrastructure.Configuration;

public static class KafkaServiceCollectionExtensions
{
    public static IServiceCollection AddKafka(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection("Kafka"))
            .Validate(
                options => !string.IsNullOrWhiteSpace(
                    options.BootstrapServers),
                "Kafka BootstrapServers is required.")
            .ValidateOnStart();

        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var options = sp
                .GetRequiredService<IOptions<KafkaOptions>>()
                .Value;

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = options.BootstrapServers
            };

            return new ProducerBuilder<string, string>(
                producerConfig)
                .Build();
        });

        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        services.AddSingleton<IProductUpdatedHandler, ProductUpdatedHandler>();

        return services;
    }
}