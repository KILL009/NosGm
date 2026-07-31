using System;
using System.Collections.Generic;
using System.Threading;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationCallbackKindParityEvidenceLedger
    {
        public const int DefaultCapacity = 16;
        public const int MaximumCapacity = 64;

        private readonly object _syncRoot = new object();
        private readonly WireV1.CommunicationCallbackKind _targetKind;
        private readonly int _capacity;
        private readonly Queue<CommunicationCallbackKindParityEvidence>
            _evidence;
        private string _processIdentity = string.Empty;
        private long _appendedEvidence;
        private long _evictedEvidence;
        private int _invalidated;

        public CommunicationCallbackKindParityEvidenceLedger(
            WireV1.CommunicationCallbackKind targetKind,
            int capacity = DefaultCapacity)
        {
            if (targetKind !=
                    WireV1.CommunicationCallbackKind.PenaltyRefresh ||
                capacity <= 0 ||
                capacity > MaximumCapacity)
            {
                throw new InvalidOperationException(
                    "The first callback qualification ledger supports only PenaltyRefresh and a bounded capacity.");
            }

            _targetKind = targetKind;
            _capacity = capacity;
            _evidence =
                new Queue<CommunicationCallbackKindParityEvidence>(capacity);
        }

        public WireV1.CommunicationCallbackKind TargetKind =>
            _targetKind;

        public int Capacity => _capacity;

        public string ProcessIdentity
        {
            get
            {
                lock (_syncRoot)
                {
                    return _processIdentity;
                }
            }
        }

        public long AppendedEvidence =>
            Interlocked.Read(ref _appendedEvidence);

        public long EvictedEvidence =>
            Interlocked.Read(ref _evictedEvidence);

        public bool IsInvalidated =>
            Volatile.Read(ref _invalidated) != 0;

        public bool HasCompleteHistory =>
            !IsInvalidated && EvictedEvidence == 0;

        public bool TryAppend(
            CommunicationCallbackKindParityEvidence evidence)
        {
            if (evidence == null)
            {
                throw new ArgumentNullException(nameof(evidence));
            }

            lock (_syncRoot)
            {
                if (IsInvalidated)
                {
                    throw new InvalidOperationException(
                        "The callback qualification ledger is invalidated for this process.");
                }
                if (evidence.Kind != _targetKind)
                {
                    InvalidateAndThrow(
                        "Callback qualification evidence belongs to a different callback kind.");
                }
                if (evidence.Verdict ==
                    CommunicationCallbackParityVerdict.InProgress)
                {
                    InvalidateAndThrow(
                        "Moving callback windows cannot enter terminal qualification evidence.");
                }

                if (string.IsNullOrEmpty(_processIdentity))
                {
                    _processIdentity = evidence.ProcessIdentity;
                }
                else if (!string.Equals(
                             _processIdentity,
                             evidence.ProcessIdentity,
                             StringComparison.Ordinal))
                {
                    InvalidateAndThrow(
                        "Callback qualification evidence crosses process identities.");
                }

                CommunicationCallbackKindParityEvidence last = null;
                foreach (CommunicationCallbackKindParityEvidence retained in
                         _evidence)
                {
                    last = retained;
                    if (!string.Equals(
                            retained.RuntimeGenerationId,
                            evidence.RuntimeGenerationId,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (EvidenceEquals(retained, evidence))
                    {
                        return false;
                    }

                    InvalidateAndThrow(
                        "A runtime generation cannot produce conflicting callback qualification evidence.");
                }

                if (last != null &&
                    evidence.ObservedAt <= last.ObservedAt)
                {
                    InvalidateAndThrow(
                        "Callback qualification evidence must be appended in terminal observation order.");
                }

                if (_evidence.Count == _capacity)
                {
                    _evidence.Dequeue();
                    Interlocked.Increment(ref _evictedEvidence);
                }
                _evidence.Enqueue(evidence);
                Interlocked.Increment(ref _appendedEvidence);
                return true;
            }
        }

        public bool Invalidate()
        {
            return Interlocked.Exchange(ref _invalidated, 1) == 0;
        }

        public IReadOnlyList<CommunicationCallbackKindParityEvidence>
            GetSnapshot()
        {
            lock (_syncRoot)
            {
                return _evidence.ToArray();
            }
        }

        public IReadOnlyList<CommunicationCallbackKindParityEvidence>
            GetLatest(int count)
        {
            if (count <= 0 || count > _capacity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    "Requested qualification evidence must fit inside the ledger capacity.");
            }

            lock (_syncRoot)
            {
                CommunicationCallbackKindParityEvidence[] snapshot =
                    _evidence.ToArray();
                int retained = Math.Min(count, snapshot.Length);
                var latest =
                    new CommunicationCallbackKindParityEvidence[retained];
                Array.Copy(
                    snapshot,
                    snapshot.Length - retained,
                    latest,
                    0,
                    retained);
                return latest;
            }
        }

        public bool TryArm(CommunicationCallbackCutoverGate gate)
        {
            if (gate == null)
            {
                throw new ArgumentNullException(nameof(gate));
            }
            if (gate.TargetKind != _targetKind)
            {
                throw new InvalidOperationException(
                    "The callback cutover gate and qualification ledger target different callback kinds.");
            }

            lock (_syncRoot)
            {
                if (IsInvalidated ||
                    Interlocked.Read(ref _evictedEvidence) != 0)
                {
                    return false;
                }
                return gate.Arm(_evidence.ToArray());
            }
        }

        private void InvalidateAndThrow(string message)
        {
            Invalidate();
            throw new InvalidOperationException(message);
        }

        private static bool EvidenceEquals(
            CommunicationCallbackKindParityEvidence left,
            CommunicationCallbackKindParityEvidence right)
        {
            return string.Equals(
                       left.ProcessIdentity,
                       right.ProcessIdentity,
                       StringComparison.Ordinal) &&
                   left.Kind == right.Kind &&
                   string.Equals(
                       left.RuntimeGenerationId,
                       right.RuntimeGenerationId,
                       StringComparison.Ordinal) &&
                   left.Verdict == right.Verdict &&
                   left.TypedLiveCount == right.TypedLiveCount &&
                   left.ScsLiveCount == right.ScsLiveCount &&
                   left.ObservedAt == right.ObservedAt;
        }
    }
}
