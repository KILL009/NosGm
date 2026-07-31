using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationCallbackShadowEnvelopeHandler
        : ICommunicationCallbackEnvelopeHandler,
          ICommunicationCallbackStreamObservationContext
    {
        public const int DefaultObservationCapacity = 4096;
        public const int MaximumObservationCapacity = 16384;

        private readonly int _capacity;
        private readonly Action<string, ulong> _streamBegan;
        private readonly Action<CommunicationCallbackReplayEvidence>
            _replayCompleted;
        private readonly Action _streamEnded;
        private readonly ICommunicationCallbackEnvelopeHandler _effectHandler;
        private readonly CommunicationCallbackOperatorCutoverCoordinator
            _cutoverCoordinator;
        private readonly Queue<CommunicationCallbackShadowObservation>
            _observations;
        private readonly object _syncRoot = new object();
        private string _runtimeGenerationId = string.Empty;
        private CommunicationCallbackReplayEvidence _replayEvidence;
        private CommunicationCallbackObservationPhase _phase;
        private long _observedCallbacks;
        private long _lastObservedSequence;
        private long _evictedObservations;
        private bool _streamActive;

        public CommunicationCallbackShadowEnvelopeHandler(
            int observationCapacity = DefaultObservationCapacity,
            Action<string, ulong> streamBegan = null,
            Action<CommunicationCallbackReplayEvidence> replayCompleted = null,
            Action streamEnded = null,
            ICommunicationCallbackEnvelopeHandler effectHandler = null,
            CommunicationCallbackOperatorCutoverCoordinator
                cutoverCoordinator = null)
        {
            if (observationCapacity <= 0 ||
                observationCapacity > MaximumObservationCapacity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(observationCapacity),
                    "Shadow observation capacity must be between 1 and " +
                    MaximumObservationCapacity + ".");
            }

            _capacity = observationCapacity;
            _streamBegan = streamBegan;
            _replayCompleted = replayCompleted;
            _streamEnded = streamEnded;
            _effectHandler = effectHandler;
            _cutoverCoordinator = cutoverCoordinator ??
                CommunicationCallbackOperatorCutoverCoordinator.Instance;
            _observations =
                new Queue<CommunicationCallbackShadowObservation>(
                    observationCapacity);
        }

        public int ObservationCapacity => _capacity;

        public bool IsStreamActive
        {
            get
            {
                lock (_syncRoot)
                {
                    return _streamActive;
                }
            }
        }

        public long ObservedCallbacks =>
            Interlocked.Read(ref _observedCallbacks);

        public ulong LastObservedSequence =>
            checked((ulong)Interlocked.Read(ref _lastObservedSequence));

        public long EvictedObservations =>
            Interlocked.Read(ref _evictedObservations);

        public void BeginStream(
            string runtimeGenerationId,
            ulong resumeAfterSequence)
        {
            if (!IsCanonicalNonEmptyGuid(runtimeGenerationId) ||
                resumeAfterSequence > (ulong)long.MaxValue)
            {
                throw new InvalidOperationException(
                    "The shadow observation stream context is invalid.");
            }

            lock (_syncRoot)
            {
                if (_streamActive)
                {
                    throw new InvalidOperationException(
                        "A shadow observation stream is already active.");
                }

                _observations.Clear();
                _runtimeGenerationId = runtimeGenerationId;
                _replayEvidence = null;
                _phase = CommunicationCallbackObservationPhase.Replay;
                Interlocked.Exchange(ref _observedCallbacks, 0);
                Interlocked.Exchange(ref _lastObservedSequence, 0);
                Interlocked.Exchange(ref _evictedObservations, 0);
                _streamActive = true;
            }

            _cutoverCoordinator
                .ObserveRuntimeGeneration(runtimeGenerationId);
            _streamBegan?.Invoke(
                runtimeGenerationId,
                resumeAfterSequence);
        }

        public void CompleteReplay(
            CommunicationCallbackReplayEvidence evidence)
        {
            if (evidence == null)
            {
                throw new ArgumentNullException(nameof(evidence));
            }

            lock (_syncRoot)
            {
                if (!_streamActive ||
                    _phase != CommunicationCallbackObservationPhase.Replay ||
                    !string.Equals(
                        _runtimeGenerationId,
                        evidence.RuntimeGenerationId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Replay evidence does not belong to the active shadow stream.");
                }
                _replayEvidence = evidence;
                _phase = CommunicationCallbackObservationPhase.Live;
            }

            _replayCompleted?.Invoke(evidence);
            _cutoverCoordinator.CompleteReplay(
                evidence.RuntimeGenerationId);
        }

        public void EndStream()
        {
            bool wasActive;
            string runtimeGenerationId;
            CommunicationCallbackReplayEvidence replayEvidence;
            long observedCallbacks;
            long evictedObservations;
            CommunicationCallbackShadowObservation[] observations;
            lock (_syncRoot)
            {
                wasActive = _streamActive;
                runtimeGenerationId = _runtimeGenerationId;
                replayEvidence = _replayEvidence;
                observedCallbacks =
                    Interlocked.Read(ref _observedCallbacks);
                evictedObservations =
                    Interlocked.Read(ref _evictedObservations);
                observations = _observations.ToArray();
                _runtimeGenerationId = string.Empty;
                _replayEvidence = null;
                _phase = 0;
                _streamActive = false;
            }

            if (!wasActive)
            {
                return;
            }

            _cutoverCoordinator.ObserveStreamEnded(
                runtimeGenerationId);
            if (_streamEnded == null)
            {
                return;
            }

            var terminalWindow =
                new CommunicationCallbackTerminalTypedObservationWindow(
                    runtimeGenerationId,
                    replayEvidence,
                    observedCallbacks,
                    evictedObservations,
                    observations,
                    DateTimeOffset.UtcNow);
            CommunicationCallbackTerminalObservationContext.Invoke(
                terminalWindow,
                _streamEnded);
        }

        public IReadOnlyList<CommunicationCallbackShadowObservation>
            GetObservationSnapshot()
        {
            lock (_syncRoot)
            {
                return _observations.ToArray();
            }
        }

        public Task ApplyAsync(
            WireV1.CommunicationCallbackEnvelope envelope,
            CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }
            cancellationToken.ThrowIfCancellationRequested();

            WireV1.CommunicationCallbackKind kind =
                CommunicationCallbackSemanticFingerprint.ResolveKind(envelope);
            string fingerprint =
                CommunicationCallbackSemanticFingerprint.Compute(envelope);
            DateTimeOffset observedAt = DateTimeOffset.UtcNow;

            lock (_syncRoot)
            {
                if (!_streamActive || _phase == 0)
                {
                    throw new InvalidOperationException(
                        "The shadow handler has no active callback stream.");
                }

                var observation = new CommunicationCallbackShadowObservation(
                    runtimeGenerationId: _runtimeGenerationId,
                    eventId: envelope.EventId,
                    sequence: envelope.Sequence,
                    kind: kind,
                    phase: _phase,
                    semanticFingerprint: fingerprint,
                    observedAt: observedAt);
                if (_observations.Count == _capacity)
                {
                    _observations.Dequeue();
                    Interlocked.Increment(ref _evictedObservations);
                }
                _observations.Enqueue(observation);
            }

            Interlocked.Exchange(
                ref _lastObservedSequence,
                checked((long)envelope.Sequence));
            Interlocked.Increment(ref _observedCallbacks);

            ICommunicationCallbackEnvelopeHandler effectHandler =
                _effectHandler ??
                CommunicationCallbackTypedEffectHandlerRegistry.Resolve();
            return effectHandler == null
                ? Task.CompletedTask
                : effectHandler.ApplyAsync(
                    envelope,
                    cancellationToken);
        }

        private static bool IsCanonicalNonEmptyGuid(string value)
        {
            return value != null &&
                   value.Length == 36 &&
                   Guid.TryParseExact(value, "D", out Guid parsed) &&
                   parsed != Guid.Empty &&
                   string.Equals(
                       parsed.ToString("D"),
                       value,
                       StringComparison.Ordinal);
        }
    }
}
