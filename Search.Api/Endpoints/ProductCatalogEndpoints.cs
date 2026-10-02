using Grpc.Core;
using ProductCatalog.Contracts;
using Search.Api.Grpc;

using ProductCatalogClient =
    ProductCatalog.Contracts.ProductCatalog.ProductCatalogClient;

namespace Search.Api.Endpoints;

public static class ProductCatalogEndpoints
{
    public static IEndpointRouteBuilder
        MapProductCatalogEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/catalog/{id}",
            async (
                string id,
                ProductCatalogClient client,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var reply =
                        await client.GetProductAsync(
                            new GetProductRequest
                            {
                                Id = id
                            },
                            deadline:
                                DateTime.UtcNow
                                    .AddSeconds(1),
                            cancellationToken:
                                cancellationToken);

                    return Results.Ok(new
                    {
                        reply.Id,
                        reply.Name,
                        reply.Description,
                        reply.Price,
                        reply.Category
                    });
                }
                catch (RpcException ex)
                {
                    return GrpcErrorMapper.Map(ex);
                }
            });

        return endpoints;
    }
}