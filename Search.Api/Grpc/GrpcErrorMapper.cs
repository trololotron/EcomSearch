using Grpc.Core;

namespace Search.Api.Grpc;

public static class GrpcErrorMapper
{
    public static IResult Map(RpcException exception)
    {
        return exception.StatusCode switch
        {
            StatusCode.InvalidArgument =>
                Results.BadRequest(new
                {
                    error = exception.Status.Detail
                }),

            StatusCode.NotFound =>
                Results.NotFound(new
                {
                    error = exception.Status.Detail
                }),

            StatusCode.Unavailable =>
                Results.Problem(
                    statusCode:
                        StatusCodes.Status503ServiceUnavailable,
                    title:
                        "Product catalog unavailable",
                    detail:
                        exception.Status.Detail),

            StatusCode.DeadlineExceeded =>
                Results.Problem(
                    statusCode:
                        StatusCodes.Status504GatewayTimeout,
                    title:
                        "Product catalog timeout",
                    detail:
                        exception.Status.Detail),

            _ =>
                Results.Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "gRPC call failed",
                    detail:
                        exception.Status.Detail)
        };
    }
}