using System;
using System.Collections.Generic;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationCallbackOverlapDeduplicationLedger
    {
        private sealed class AppliedEffect
        {
            public CommunicationCallbackParitySource Source { get; set; }

            public string SemanticFingerprint { get; set; }

            public DateTimeOffset AppliedAt { get; set; }
        }

        public const int DefaultCapacity = 1024;
        public const int MaximumCapacity = 4096;
        public static readonly TimeSpan DefaultRetention =
            TimeSpan.FromMinutes(10);

        private readonly int _capacity;
        private readonly TimeSpan _retention;
        private readonly LinkedList<AppliedEffect> _pending =
            new LinkedList<AppliedEffect>();
        private long _recorded;
        private long _duplicatesSuppressed;
        private long _expired;

        public CommunicationCallbackOverlapDeduplicationLedger(
            int capacity = DefaultCapacity,
            TimeSpan? retention = null)
        {
            TimeSpan selectedRetention = retention ?? DefaultRetention;
            if (capacity <= 0 || capacity > MaximumCapacity ||
                selectedRetention <= TimeSpan.Zero ||
                selectedRetention > TimeSpan.FromHours(1))
            {
                throw new InvalidOperationException(
                    "Callback overlap retention must use bounded capacity and duration.");
            }

            _capacity = capacity;
            _retention = selectedRetention;
        }

        public int Capacity => _capacity;

        public int PendingCount => _pending.Count;

        public long Recorded => _recorded;

        public long DuplicatesSuppressed => _duplicatesSuppressed;

        public long Expired => _expired;

        public bool HasCapacity(DateTimeOffset observedAt)
        {
            Prune(observedAt);
            return _pending.Count < _capacity;
        }

        public bool TryConsumeOpposite(
            CommunicationCallbackParitySource source,
            string semanticFingerprint,
            DateTimeOffset observedAt)
        {
            Validate(source, semanticFingerprint, observedAt);
            Prune(observedAt);
            CommunicationCallbackParitySource opposite =
                source == CommunicationCallbackParitySource.LegacyScs
                    ? CommunicationCallbackParitySource.TypedGrpc
                    : CommunicationCallbackParitySource.LegacyScs;

            LinkedListNode<AppliedEffect> node = _pending.First;
            while (node != null)
            {
                LinkedListNode<AppliedEffect> next = node.Next;
                AppliedEffect item = node.Value;
                if (item.Source == opposite &&
                    string.Equals(
                        item.SemanticFingerprint,
                        semanticFingerprint,
                        StringComparison.Ordinal))
                {
                    _pending.Remove(node);
                    _duplicatesSuppressed++;
                    return true;
                }
                node = next;
            }

            return false;
        }

        public void RecordApplied(
            CommunicationCallbackParitySource source,
            string semanticFingerprint,
            DateTimeOffset appliedAt)
        {
            Validate(source, semanticFingerprint, appliedAt);
            Prune(appliedAt);
            if (_pending.Count >= _capacity)
            {
                throw new InvalidOperationException(
                    "Callback overlap evidence reached its bounded capacity.");
            }

            _pending.AddLast(
                new AppliedEffect
                {
                    Source = source,
                    SemanticFingerprint = semanticFingerprint,
                    AppliedAt = appliedAt.ToUniversalTime()
                });
            _recorded++;
        }

        private void Prune(DateTimeOffset observedAt)
        {
            DateTimeOffset cutoff = observedAt.ToUniversalTime() - _retention;
            LinkedListNode<AppliedEffect> node = _pending.First;
            while (node != null && node.Value.AppliedAt <= cutoff)
            {
                LinkedListNode<AppliedEffect> next = node.Next;
                _pending.Remove(node);
                _expired++;
                node = next;
            }
        }

        private static void Validate(
            CommunicationCallbackParitySource source,
            string semanticFingerprint,
            DateTimeOffset observedAt)
        {
            if (!Enum.IsDefined(
                    typeof(CommunicationCallbackParitySource),
                    source) ||
                source == CommunicationCallbackParitySource.Unspecified ||
                string.IsNullOrWhiteSpace(semanticFingerprint) ||
                semanticFingerprint.Length > 256 ||
                !string.Equals(
                    semanticFingerprint,
                    semanticFingerprint.Trim(),
                    StringComparison.Ordinal) ||
                observedAt == default(DateTimeOffset))
            {
                throw new InvalidOperationException(
                    "Callback overlap evidence is malformed.");
            }
        }
    }
}
