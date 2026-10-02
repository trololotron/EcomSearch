using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductCatalog.Grpc.Services;
using Search.Application.Abstractions;

namespace ProductCatalog.Grpc.Tests;

internal sealed class ProductCatalogGrpcFactory
    : WebApplicationFactory<ProductCatalogService>
{
    public FakeProductRepository Repository { get; } = new();

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IProductRepository>();

            services.AddSingleton<IProductRepository>(
                Repository);
        });
    }
}