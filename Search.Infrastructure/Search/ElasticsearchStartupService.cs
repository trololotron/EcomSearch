using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Search.Infrastructure.Search;

public sealed class ElasticsearchStartupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public ElasticsearchStartupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var initializer = scope.ServiceProvider
            .GetRequiredService<ElasticsearchIndexInitializer>();

        await initializer.InitializeAsync(cancellationToken);

        var seeder = scope.ServiceProvider
            .GetRequiredService<ProductSeeder>();

        await seeder.SeedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}