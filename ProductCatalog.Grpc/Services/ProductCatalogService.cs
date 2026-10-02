using Grpc.Core;
using Search.Application.Products;
using ProductCatalog.Contracts;

using ProductCatalogContract =
    global::ProductCatalog.Contracts.ProductCatalog;

namespace ProductCatalog.Grpc.Services;

public sealed class ProductCatalogService(
    GetProductById getProductById)
    : ProductCatalogContract.ProductCatalogBase
{
    public override async Task<GetProductReply> GetProduct(
        GetProductRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var productId))
        {
            throw new RpcException(
                new Status(
                    StatusCode.InvalidArgument,
                    "Product id must be a valid GUID."));
        }

        var product = await getProductById.ExecuteAsync(
            productId,
            context.CancellationToken);

        if (product is null)
        {
            throw new RpcException(
                new Status(
                    StatusCode.NotFound,
                    $"Product '{productId}' was not found."));
        }

        return new GetProductReply
        {
            Id = product.Id.ToString(),
            Name = product.Name,
            Description = product.Description,
            Price = product.Price.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            Category = product.Category
        };
    }
}