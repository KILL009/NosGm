using Grpc.Core;
using NosGm.Cluster.Contracts.V1;

namespace NosGm.Authentication.Server.Services;

public sealed class AuthenticationDispatchGate : IDisposable
{
    private readonly SemaphoreSlim _concurrency =
        new(ClusterProtocolLimits.MaxConcurrentCallsPerConnection);
    private int _queuedCalls;

    public async Task<T> RunAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        int queued = Interlocked.Increment(ref _queuedCalls);
        if (queued > ClusterProtocolLimits.BoundedDispatchQueueCapacity)
        {
            Interlocked.Decrement(ref _queuedCalls);
            throw new RpcException(
                new Status(
                    StatusCode.ResourceExhausted,
                    "Authentication dispatch queue is full."));
        }

        try
        {
            await _concurrency.WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Decrement(ref _queuedCalls);
        }

        try
        {
            return await action(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _concurrency.Release();
        }
    }

    public void Dispose()
    {
        _concurrency.Dispose();
    }
}
