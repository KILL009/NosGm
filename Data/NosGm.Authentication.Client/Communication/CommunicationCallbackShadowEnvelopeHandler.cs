using System;
using System.Threading;
using System.Threading.Tasks;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationCallbackShadowEnvelopeHandler
        : ICommunicationCallbackEnvelopeHandler
    {
        private long _observedCallbacks;
        private ulong _lastObservedSequence;

        public long ObservedCallbacks =>
            Interlocked.Read(ref _observedCallbacks);

        public ulong LastObservedSequence =>
            Volatile.Read(ref _lastObservedSequence);

        public Task ApplyAsync(
            WireV1.CommunicationCallbackEnvelope envelope,
            CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _lastObservedSequence, envelope.Sequence);
            Interlocked.Increment(ref _observedCallbacks);
            return Task.CompletedTask;
        }
    }
}
