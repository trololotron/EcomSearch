using Grpc.Core;
using Grpc.Net.Client.Configuration;

namespace Search.Api.Grpc;

public static class ProductCatalogGrpcPolicy
{
    public static ServiceConfig CreateRetryConfig()
    {
        return new ServiceConfig
        {
            MethodConfigs =
            {
                new MethodConfig
                {
                    Names =
                    {
                        MethodName.Default
                    },
                    RetryPolicy = new RetryPolicy
                    {
                        MaxAttempts = 3,
                        InitialBackoff =
                            TimeSpan.FromMilliseconds(100),
                        MaxBackoff =
                            TimeSpan.FromMilliseconds(500),
                        BackoffMultiplier = 2,
                        RetryableStatusCodes =
                        {
                            StatusCode.Unavailable
                        }
                    }
                }
            }
        };
    }
}