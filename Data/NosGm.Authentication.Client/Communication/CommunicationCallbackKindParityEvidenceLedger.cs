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
        private long _retainedEvidence;
        private long _evictedEvidence;

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

        public long RetainedEvidence =>
            Interlocked.Read(ref _retainedEvidence);

        public long EvictedEvidence =>
            Interlocked.Read(ref _evictedEvidence);

        public bool TryAppend(
            CommunicationCallbackKindParityEvidence evidence)
        {
            if (evidence == null)
            {
                throw new ArgumentNullException(nameof(evidence));
            }
            if (evidence.Kind != _targetKind)
            {
                throw new InvalidOperationException(
                    "Callback qualification evidence belongs to a different callback kind.");
            }
            if (evidence.Verdict ==
                CommunicationCallbackParityVerdict.InProgress)
            {
                throw new InvalidOperationException(
                    "Moving callback windows cannot enter terminal qualification evidence.");
            }

            lock (_syncRoot)
            {
                if (string.IsNullOrEmpty(_processIdentity))
                {
                    _processIdentity = evidence.ProcessIdentity;
                }
                else if (!string.Equals(
                             _processIdentity,
                             evidence.ProcessIdentity,
                             StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
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

                    throw new InvalidOperationException(
                        "A runtime generation cannot produce conflicting callback qualification evidence.");
                }

                if (last != null &&
                    evidence.ObservedAt <= last.ObservedAt)
                {
                    throw new InvalidOperationException(
                        "Callback qualification evidence must be appended in terminal observation order.");
                }

                if (_evidence.Count == _capacity)
                {
                    _evidence.Dequeue();
                    Interlocked.Increment(ref _evictedEvidence);
                }
                _evidence.Enqueue(evidence);
                Interlocked.Increment(ref _retainedEvidence);
                return true;
            }
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

            return gate.Arm(GetSnapshot());
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
