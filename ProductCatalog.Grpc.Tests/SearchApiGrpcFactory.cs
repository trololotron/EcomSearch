using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Search.Api.Controllers;

using ProductCatalogClient =
    ProductCatalog.Contracts.ProductCatalog.ProductCatalogClient;

namespace ProductCatalog.Grpc.Tests;

internal sealed class SearchApiGrpcFactory
    : WebApplicationFactory<ProductsController>
{
    private readonly ProductCatalogGrpcFactory _grpcFactory;

    public SearchApiGrpcFactory(
        ProductCatalogGrpcFactory grpcFactory)
    {
        _grpcFactory = grpcFactory;
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ProductCatalogClient>();

            services.AddSingleton(sp =>
            {
                var handler =
                    _grpcFactory.Server.CreateHandler();

                var channel =
                    GrpcChannel.ForAddress(
                        "http://localhost",
                        new GrpcChannelOptions
                        {
                            HttpHandler = handler
                        });

                return new ProductCatalogClient(channel);
            });
        });
    }
}