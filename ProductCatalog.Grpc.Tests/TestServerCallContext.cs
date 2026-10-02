using Grpc.Core;

namespace ProductCatalog.Grpc.Tests;

internal sealed class TestServerCallContext
    : ServerCallContext
{
    private readonly CancellationToken _cancellationToken;
    private readonly Metadata _requestHeaders = new();
    private readonly Metadata _responseTrailers = new();

    private Status _status;
    private WriteOptions? _writeOptions;

    public TestServerCallContext(
        CancellationToken cancellationToken = default)
    {
        _cancellationToken = cancellationToken;
    }

    protected override string MethodCore =>
        "test";

    protected override string HostCore =>
        "localhost";

    protected override string PeerCore =>
        "127.0.0.1";

    protected override DateTime DeadlineCore =>
        DateTime.MaxValue;

    protected override Metadata RequestHeadersCore =>
        _requestHeaders;

    protected override CancellationToken CancellationTokenCore =>
        _cancellationToken;

    protected override Metadata ResponseTrailersCore =>
        _responseTrailers;

    protected override Status StatusCore
    {
        get => _status;
        set => _status = value;
    }

    protected override WriteOptions? WriteOptionsCore
    {
        get => _writeOptions;
        set => _writeOptions = value;
    }

    protected override AuthContext AuthContextCore =>
        null!;

    protected override Task WriteResponseHeadersAsyncCore(
        Metadata responseHeaders)
    {
        return Task.CompletedTask;
    }

    protected override ContextPropagationToken
        CreatePropagationTokenCore(
            ContextPropagationOptions? options)
    {
        throw new NotSupportedException();
    }
}