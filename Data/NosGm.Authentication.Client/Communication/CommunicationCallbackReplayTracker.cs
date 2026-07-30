using System;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationCallbackReplayEvidence
    {
        internal CommunicationCallbackReplayEvidence(
            string runtimeGenerationId,
            ulong replayThroughSequence,
            ulong resumeAfterSequence,
            uint replayedEvents,
            DateTimeOffset completedAt)
        {
            RuntimeGenerationId = runtimeGenerationId;
            ReplayThroughSequence = replayThroughSequence;
            ResumeAfterSequence = resumeAfterSequence;
            ReplayedEvents = replayedEvents;
            CompletedAt = completedAt;
        }

        public string RuntimeGenerationId { get; }

        public ulong ReplayThroughSequence { get; }

        public ulong ResumeAfterSequence { get; }

        public uint ReplayedEvents { get; }

        public DateTimeOffset CompletedAt { get; }
    }

    public sealed class CommunicationCallbackReplayTracker
    {
        private readonly object _syncRoot = new object();
        private CommunicationCallbackReplayEvidence _evidence;
        private string _runtimeGenerationId = string.Empty;
        private ulong _resumeAfterSequence;
        private ulong _lastReplaySequence;
        private uint _observedReplayEvents;
        private bool _streamStarted;

        public CommunicationCallbackReplayEvidence Evidence
        {
            get
            {
                lock (_syncRoot)
                {
                    return _evidence;
                }
            }
        }

        public bool IsComplete
        {
            get
            {
                lock (_syncRoot)
                {
                    return _evidence != null;
                }
            }
        }

        public void Reset()
        {
            lock (_syncRoot)
            {
                _runtimeGenerationId = string.Empty;
                _resumeAfterSequence = 0;
                _lastReplaySequence = 0;
                _observedReplayEvents = 0;
                _evidence = null;
                _streamStarted = false;
            }
        }

        public void BeginStream(
            string runtimeGenerationId,
            ulong resumeAfterSequence)
        {
            if (!IsCanonicalNonEmptyGuid(runtimeGenerationId))
            {
                throw new InvalidOperationException(
                    "The callback replay tracker received an invalid runtime generation.");
            }
            if (resumeAfterSequence > (ulong)long.MaxValue)
            {
                throw new InvalidOperationException(
                    "The callback replay cursor exceeds the supported range.");
            }

            lock (_syncRoot)
            {
                _runtimeGenerationId = runtimeGenerationId;
                _resumeAfterSequence = resumeAfterSequence;
                _lastReplaySequence = resumeAfterSequence;
                _observedReplayEvents = 0;
                _evidence = null;
                _streamStarted = true;
            }
        }

        public void ObserveCallbackBeforeBarrier(ulong sequence)
        {
            lock (_syncRoot)
            {
                RequireStarted();
                if (_evidence != null)
                {
                    throw new InvalidOperationException(
                        "A callback was classified as replay after the replay barrier.");
                }
                if (sequence <= _lastReplaySequence ||
                    sequence > (ulong)long.MaxValue)
                {
                    throw new InvalidOperationException(
                        "The callback replay sequence is not strictly increasing.");
                }
                if (_observedReplayEvents == uint.MaxValue)
                {
                    throw new InvalidOperationException(
                        "The callback replay event count was exhausted.");
                }

                _lastReplaySequence = sequence;
                _observedReplayEvents++;
            }
        }

        public CommunicationCallbackReplayEvidence Complete(
            WireV1.CommunicationCallbackEnvelope envelope,
            DateTimeOffset completedAt)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            lock (_syncRoot)
            {
                RequireStarted();
                if (_evidence != null)
                {
                    throw new InvalidOperationException(
                        "The callback stream returned more than one replay barrier.");
                }
                if (envelope.CallbackCase != WireV1
                        .CommunicationCallbackEnvelope.CallbackOneofCase
                        .ReplayComplete ||
                    envelope.ReplayComplete == null ||
                    !string.IsNullOrEmpty(envelope.EventId) ||
                    envelope.IssuedAtUnixTimeMs != 0 ||
                    envelope.ExpiresAtUnixTimeMs != 0 ||
                    envelope.Target != null)
                {
                    throw new InvalidOperationException(
                        "The callback replay barrier contains event metadata.");
                }

                WireV1.CommunicationCallbackReplayComplete barrier =
                    envelope.ReplayComplete;
                if (!IsCanonicalNonEmptyGuid(barrier.RuntimeGenerationId) ||
                    !string.Equals(
                        barrier.RuntimeGenerationId,
                        _runtimeGenerationId,
                        StringComparison.Ordinal) ||
                    barrier.ResumeAfterSequence != _resumeAfterSequence ||
                    barrier.ReplayThroughSequence < _resumeAfterSequence ||
                    barrier.ReplayThroughSequence > (ulong)long.MaxValue ||
                    envelope.Sequence != barrier.ReplayThroughSequence ||
                    barrier.ReplayedEvents != _observedReplayEvents ||
                    _lastReplaySequence > barrier.ReplayThroughSequence)
                {
                    throw new InvalidOperationException(
                        "The callback replay barrier does not match the observed stream.");
                }

                _evidence = new CommunicationCallbackReplayEvidence(
                    barrier.RuntimeGenerationId,
                    barrier.ReplayThroughSequence,
                    barrier.ResumeAfterSequence,
                    barrier.ReplayedEvents,
                    completedAt);
                return _evidence;
            }
        }

        public void ValidateLiveSequence(ulong sequence)
        {
            lock (_syncRoot)
            {
                RequireStarted();
                if (_evidence == null)
                {
                    throw new InvalidOperationException(
                        "The callback stream has not crossed its replay barrier.");
                }
                if (sequence <= _evidence.ReplayThroughSequence ||
                    sequence > (ulong)long.MaxValue)
                {
                    throw new InvalidOperationException(
                        "A live callback did not follow the replay boundary.");
                }
            }
        }

        private void RequireStarted()
        {
            if (!_streamStarted)
            {
                throw new InvalidOperationException(
                    "The callback replay tracker has no active stream.");
            }
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
