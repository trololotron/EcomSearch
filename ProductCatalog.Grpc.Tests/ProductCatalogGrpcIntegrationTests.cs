using Grpc.Core;
using Grpc.Net.Client;
using ProductCatalog.Contracts;
using Search.Api.Grpc;
using Search.Domain;

using ProductCatalogClient =
    ProductCatalog.Contracts.ProductCatalog.ProductCatalogClient;

namespace ProductCatalog.Grpc.Tests;

public sealed class ProductCatalogGrpcIntegrationTests
{
    [Fact]
    public async Task GetProduct_WhenProductExists_ReturnsProduct()
    {
        await using var factory =
            new ProductCatalogGrpcFactory();

        var productId = Guid.NewGuid();

        factory.Repository.ProductToReturn =
            new Product
            {
                Id = productId,
                Name = "Integration iPhone",
                Description = "Real gRPC integration test",
                Price = 123456.78m,
                Category = "Phones"
            };

        var handler =
            factory.Server.CreateHandler();

        using var channel =
            GrpcChannel.ForAddress(
                "http://localhost",
                new GrpcChannelOptions
                {
                    HttpHandler = handler
                });

        var client =
            new ProductCatalogClient(channel);

        var reply =
            await client.GetProductAsync(
                new GetProductRequest
                {
                    Id = productId.ToString()
                });

        Assert.Equal(
            productId.ToString(),
            reply.Id);

        Assert.Equal(
            "Integration iPhone",
            reply.Name);

        Assert.Equal(
            "123456.78",
            reply.Price);
    }

    [Fact]
    public async Task GetProduct_WhenIdInvalid_ReturnsInvalidArgument()
    {
        await using var factory =
            new ProductCatalogGrpcFactory();

        var handler =
            factory.Server.CreateHandler();

        using var channel =
            GrpcChannel.ForAddress(
                "http://localhost",
                new GrpcChannelOptions
                {
                    HttpHandler = handler
                });

        var client =
            new ProductCatalogClient(channel);

        var exception =
            await Assert.ThrowsAsync<RpcException>(
                async () =>
                {
                    await client.GetProductAsync(
                        new GetProductRequest
                        {
                            Id = "not-a-guid"
                        });
                });

        Assert.Equal(
            StatusCode.InvalidArgument,
            exception.StatusCode);
    }

    [Fact]
    public async Task GetProduct_WhenProductMissing_ReturnsNotFound()
    {
        await using var factory =
            new ProductCatalogGrpcFactory();

        factory.Repository.ProductToReturn = null;

        var productId = Guid.NewGuid();

        var handler =
            factory.Server.CreateHandler();

        using var channel =
            GrpcChannel.ForAddress(
                "http://localhost",
                new GrpcChannelOptions
                {
                    HttpHandler = handler
                });

        var client =
            new ProductCatalogClient(channel);

        var exception =
            await Assert.ThrowsAsync<RpcException>(
                async () =>
                {
                    await client.GetProductAsync(
                        new GetProductRequest
                        {
                            Id = productId.ToString()
                        });
                });

        Assert.Equal(
            StatusCode.NotFound,
            exception.StatusCode);
    }

    [Fact]
    public async Task GetProduct_WhenFirstTwoAttemptsUnavailable_RetriesAndSucceeds()
    {
        await using var factory =
            new ProductCatalogGrpcFactory();

        var productId = Guid.NewGuid();

        var attempts = 0;

        factory.Repository.GetByIdHandler =
            (_, _) =>
            {
                attempts++;

                if (attempts <= 2)
                {
                    throw new RpcException(
                        new Status(
                            StatusCode.Unavailable,
                            $"Temporary failure {attempts}"));
                }

                return Task.FromResult<Product?>(
                    new Product
                    {
                        Id = productId,
                        Name = "Retry iPhone",
                        Description = "Retry integration test",
                        Price = 999m,
                        Category = "Phones"
                    });
            };

        var handler =
            factory.Server.CreateHandler();

        using var channel =
            GrpcChannel.ForAddress(
                "http://localhost",
                new GrpcChannelOptions
                {
                    HttpHandler = handler,
                    ServiceConfig =
                        ProductCatalogGrpcPolicy.CreateRetryConfig()
                });

        var client =
            new ProductCatalogClient(channel);

        var reply =
            await client.GetProductAsync(
                new GetProductRequest
                {
                    Id = productId.ToString()
                });

        Assert.Equal(
            3,
            attempts);

        Assert.Equal(
            productId.ToString(),
            reply.Id);

        Assert.Equal(
            "Retry iPhone",
            reply.Name);
    }
}