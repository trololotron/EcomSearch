using Grpc.Core;
using ProductCatalog.Contracts;
using ProductCatalog.Grpc.Services;
using Search.Application.Products;
using Search.Domain;

namespace ProductCatalog.Grpc.Tests;

public sealed class ProductCatalogServiceTests
{
    [Fact]
    public async Task GetProduct_WhenProductExists_ReturnsProduct()
    {
        var productId = Guid.NewGuid();

        var repository = new FakeProductRepository
        {
            ProductToReturn = new Product
            {
                Id = productId,
                Name = "iPhone",
                Description = "Test phone",
                Price = 123456.78m,
                Category = "Phones"
            }
        };

        var getProductById =
            new GetProductById(repository);

        var service =
            new ProductCatalogService(getProductById);

        var context =
            new TestServerCallContext();

        var result = await service.GetProduct(
            new GetProductRequest
            {
                Id = productId.ToString()
            },
            context);

        Assert.Equal(
            productId.ToString(),
            result.Id);

        Assert.Equal(
            "iPhone",
            result.Name);

        Assert.Equal(
            "Test phone",
            result.Description);

        Assert.Equal(
            "123456.78",
            result.Price);

        Assert.Equal(
            "Phones",
            result.Category);

        Assert.Equal(
            productId,
            repository.RequestedId);
    }

    [Fact]
    public async Task GetProduct_WhenIdIsInvalid_ThrowsInvalidArgument()
    {
        var repository =
            new FakeProductRepository();

        var getProductById =
            new GetProductById(repository);

        var service =
            new ProductCatalogService(getProductById);

        var context =
            new TestServerCallContext();

        var exception =
            await Assert.ThrowsAsync<RpcException>(
                () => service.GetProduct(
                    new GetProductRequest
                    {
                        Id = "not-a-guid"
                    },
                    context));

        Assert.Equal(
            StatusCode.InvalidArgument,
            exception.StatusCode);

        Assert.Null(repository.RequestedId);
    }

    [Fact]
    public async Task GetProduct_WhenProductDoesNotExist_ThrowsNotFound()
    {
        var productId = Guid.NewGuid();

        var repository = new FakeProductRepository
        {
            ProductToReturn = null
        };

        var getProductById =
            new GetProductById(repository);

        var service =
            new ProductCatalogService(getProductById);

        var context =
            new TestServerCallContext();

        var exception =
            await Assert.ThrowsAsync<RpcException>(
                () => service.GetProduct(
                    new GetProductRequest
                    {
                        Id = productId.ToString()
                    },
                    context));

        Assert.Equal(
            StatusCode.NotFound,
            exception.StatusCode);

        Assert.Equal(
            productId,
            repository.RequestedId);
    }
}