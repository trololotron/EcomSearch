using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Search.Api.Grpc;

namespace Search.Application.Tests;

public sealed class GrpcErrorMapperTests
{
    [Theory]
    [InlineData(StatusCode.InvalidArgument, 400)]
    [InlineData(StatusCode.NotFound, 404)]
    [InlineData(StatusCode.Unavailable, 503)]
    [InlineData(StatusCode.DeadlineExceeded, 504)]
    public async Task Map_ReturnsExpectedHttpStatus(
        StatusCode grpcStatus,
        int expectedHttpStatus)
    {
        var exception = new RpcException(
            new Status(
                grpcStatus,
                "Test error"));

        var result = GrpcErrorMapper.Map(exception);

        var statusResult =
            Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);

        Assert.Equal(
            expectedHttpStatus,
            statusResult.StatusCode);
    }
}
