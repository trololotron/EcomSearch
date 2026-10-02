using ProductCatalog.Contracts;
using Search.Api.Grpc;

using ProductCatalogClient =
    ProductCatalog.Contracts.ProductCatalog.ProductCatalogClient;

namespace Search.Api.Configuration;

public static class ProductCatalogGrpcExtensions
{
    public static IServiceCollection
        AddProductCatalogGrpcClient(
            this IServiceCollection services,
            IConfiguration configuration)
    {
        var address =
            configuration[
                "ProductCatalogGrpc:Address"]
            ?? throw new InvalidOperationException(
                "ProductCatalogGrpc:Address is not configured.");

        services
            .AddGrpcClient<ProductCatalogClient>(
                options =>
                {
                    options.Address =
                        new Uri(address);
                })
            .ConfigureChannel(options =>
            {
                options.ServiceConfig =
                    ProductCatalogGrpcPolicy
                        .CreateRetryConfig();
            });

        return services;
    }
}