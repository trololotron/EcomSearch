using Grpc.Core;
using Search.Domain;
using System.Net;

namespace ProductCatalog.Grpc.Tests;

public sealed class SearchApiGrpcIntegrationTests
{
    [Fact]
    public async Task GetCatalog_WhenProductExists_Returns200()
    {
        await using var grpcFactory =
            new ProductCatalogGrpcFactory();

        var productId = Guid.NewGuid();

        grpcFactory.Repository.ProductToReturn =
            new Product
            {
                Id = productId,
                Name = "Integration iPhone",
                Description = "REST to gRPC integration test",
                Price = 123456.78m,
                Category = "Phones"
            };

        await using var apiFactory =
            new SearchApiGrpcFactory(grpcFactory);

        using var client =
            apiFactory.CreateClient();

        var response =
            await client.GetAsync(
                $"/catalog/{productId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCatalog_WhenIdInvalid_Returns400()
    {
        await using var grpcFactory =
            new ProductCatalogGrpcFactory();

        await using var apiFactory =
            new SearchApiGrpcFactory(grpcFactory);

        using var client =
            apiFactory.CreateClient();

        var response =
            await client.GetAsync(
                "/catalog/not-a-guid");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCatalog_WhenProductMissing_Returns404()
    {
        await using var grpcFactory =
            new ProductCatalogGrpcFactory();

        grpcFactory.Repository.ProductToReturn = null;

        await using var apiFactory =
            new SearchApiGrpcFactory(grpcFactory);

        using var client =
            apiFactory.CreateClient();

        var productId = Guid.NewGuid();

        var response =
            await client.GetAsync(
                $"/catalog/{productId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCatalog_WhenGrpcUnavailable_Returns503()
    {
        await using var grpcFactory =
            new ProductCatalogGrpcFactory();

        grpcFactory.Repository.GetByIdHandler =
            (_, _) =>
                throw new RpcException(
                    new Status(
                        StatusCode.Unavailable,
                        "Temporary failure"));

        await using var apiFactory =
            new SearchApiGrpcFactory(grpcFactory);

        using var client =
            apiFactory.CreateClient();

        var productId = Guid.NewGuid();

        var response =
            await client.GetAsync(
                $"/catalog/{productId}");

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCatalog_WhenGrpcDeadlineExceeded_Returns504()
    {
        await using var grpcFactory =
            new ProductCatalogGrpcFactory();

        grpcFactory.Repository.GetByIdHandler =
            (_, _) =>
                throw new RpcException(
                    new Status(
                        StatusCode.DeadlineExceeded,
                        "Test deadline exceeded"));

        await using var apiFactory =
            new SearchApiGrpcFactory(grpcFactory);

        using var client =
            apiFactory.CreateClient();

        var productId = Guid.NewGuid();

        var response =
            await client.GetAsync(
                $"/catalog/{productId}");

        Assert.Equal(
            HttpStatusCode.GatewayTimeout,
            response.StatusCode);
    }
}