using System;
using System.Threading;
using System.Threading.Tasks;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationCallbackProcessor
    {
        private readonly ICommunicationCallbackCursorStore _cursorStore;
        private readonly ICommunicationCallbackEnvelopeHandler _handler;
        private ulong _appliedSequence;

        public CommunicationCallbackProcessor(
            ICommunicationCallbackCursorStore cursorStore,
            ICommunicationCallbackEnvelopeHandler handler)
        {
            _cursorStore = cursorStore ??
                throw new ArgumentNullException(nameof(cursorStore));
            _handler = handler ??
                throw new ArgumentNullException(nameof(handler));
            _appliedSequence = _cursorStore.Load();
        }

        public ulong AppliedSequence => _appliedSequence;

        public async Task<bool> ProcessAsync(
            WireV1.CommunicationCallbackEnvelope envelope,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            if (envelope == null || envelope.Sequence == 0)
            {
                throw new InvalidOperationException(
                    "The callback stream returned an invalid envelope.");
            }
            if (envelope.Sequence <= _appliedSequence)
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (envelope.ExpiresAtUnixTimeMs > now.ToUnixTimeMilliseconds())
            {
                await _handler.ApplyAsync(envelope, cancellationToken)
                    .ConfigureAwait(false);
            }

            // Commit only after application. A failed handler leaves the event
            // eligible for replay from the previously durable sequence.
            _cursorStore.Save(envelope.Sequence);
            _appliedSequence = envelope.Sequence;
            return true;
        }
    }
}
