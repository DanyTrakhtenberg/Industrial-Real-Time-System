using Grpc.Core;

namespace SqlDataService.Tests.TestSupport;

/// <summary>
/// Factory for a minimal gRPC <see cref="ServerCallContext"/> used in unit tests.
/// </summary>
public static class TestGrpcCallContext
{
    public static ServerCallContext Create(CancellationToken cancellationToken = default) =>
        new TestServerCallContext(cancellationToken);
}
