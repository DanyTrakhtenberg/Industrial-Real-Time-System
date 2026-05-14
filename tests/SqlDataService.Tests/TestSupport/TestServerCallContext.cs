using System.Collections.Generic;
using Grpc.Core;

namespace SqlDataService.Tests.TestSupport;

/// <summary>
/// Minimal <see cref="ServerCallContext"/> for unit tests (only CancellationToken is used by SqlData gRPC services).
/// </summary>
public sealed class TestServerCallContext : ServerCallContext
{
    private readonly CancellationToken _cancellationToken;
    private Status _status = Status.DefaultSuccess;
    private WriteOptions? _writeOptions;

    public TestServerCallContext(CancellationToken cancellationToken = default)
    {
        _cancellationToken = cancellationToken;
    }

    protected override string MethodCore => "TestMethod";

    protected override string HostCore => "localhost";

    protected override string PeerCore => "ipv4:127.0.0.1:0";

    protected override DateTime DeadlineCore => DateTime.MaxValue;

    protected override Metadata RequestHeadersCore => Metadata.Empty;

    protected override CancellationToken CancellationTokenCore => _cancellationToken;

    protected override Metadata ResponseTrailersCore => Metadata.Empty;

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

    protected override AuthContext AuthContextCore => new AuthContext(string.Empty, new Dictionary<string, List<AuthProperty>>());

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
        throw new NotSupportedException();

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) =>
        Task.CompletedTask;
}
