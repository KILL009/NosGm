using System;
using System.Collections.Generic;

namespace NosGm.Authentication.Client.Configuration
{
    public sealed class ConfigurationUpdateOverlapDeduplicationLedger
    {
        private sealed class AppliedUpdate
        {
            public ConfigurationAuthoritySource Source { get; set; }

            public string SemanticFingerprint { get; set; }

            public DateTimeOffset AppliedAt { get; set; }
        }

        public const int DefaultCapacity = 256;
        public const int MaximumCapacity = 4096;
        public static readonly TimeSpan DefaultRetention =
            TimeSpan.FromMinutes(10);

        private readonly int _capacity;
        private readonly LinkedList<AppliedUpdate> _pending =
            new LinkedList<AppliedUpdate>();
        private readonly TimeSpan _retention;
        private readonly object _syncRoot = new object();
        private long _duplicatesSuppressed;
        private long _expired;
        private long _recorded;

        public ConfigurationUpdateOverlapDeduplicationLedger(
            int capacity = DefaultCapacity,
            TimeSpan? retention = null)
        {
            TimeSpan selectedRetention = retention ?? DefaultRetention;
            if (capacity <= 0 || capacity > MaximumCapacity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Configuration overlap evidence requires bounded capacity.");
            }
            if (selectedRetention <= TimeSpan.Zero ||
                selectedRetention > TimeSpan.FromHours(1))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(retention),
                    "Configuration overlap evidence requires bounded retention.");
            }

            _capacity = capacity;
            _retention = selectedRetention;
        }

        public int Capacity => _capacity;

        public int PendingCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _pending.Count;
                }
            }
        }

        public long Recorded
        {
            get
            {
                lock (_syncRoot)
                {
                    return _recorded;
                }
            }
        }

        public long DuplicatesSuppressed
        {
            get
            {
                lock (_syncRoot)
                {
                    return _duplicatesSuppressed;
                }
            }
        }

        public long Expired
        {
            get
            {
                lock (_syncRoot)
                {
                    return _expired;
                }
            }
        }

        public bool HasCapacity(DateTimeOffset observedAt)
        {
            ValidateObservedAt(observedAt);
            lock (_syncRoot)
            {
                PruneLocked(observedAt);
                return _pending.Count < _capacity;
            }
        }

        public bool TryConsumeOpposite(
            ConfigurationAuthoritySource source,
            string semanticFingerprint,
            DateTimeOffset observedAt)
        {
            Validate(source, semanticFingerprint, observedAt);
            lock (_syncRoot)
            {
                PruneLocked(observedAt);
                ConfigurationAuthoritySource opposite =
                    source == ConfigurationAuthoritySource.Scs
                        ? ConfigurationAuthoritySource.TypedGrpc
                        : ConfigurationAuthoritySource.Scs;
                LinkedListNode<AppliedUpdate> node = _pending.First;
                while (node != null)
                {
                    LinkedListNode<AppliedUpdate> next = node.Next;
                    AppliedUpdate item = node.Value;
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
        }

        public void RecordApplied(
            ConfigurationAuthoritySource source,
            string semanticFingerprint,
            DateTimeOffset appliedAt)
        {
            Validate(source, semanticFingerprint, appliedAt);
            lock (_syncRoot)
            {
                PruneLocked(appliedAt);
                if (_pending.Count >= _capacity)
                {
                    throw new InvalidOperationException(
                        "Configuration overlap evidence reached its bounded capacity.");
                }

                _pending.AddLast(
                    new AppliedUpdate
                    {
                        Source = source,
                        SemanticFingerprint = semanticFingerprint,
                        AppliedAt = appliedAt.ToUniversalTime()
                    });
                _recorded++;
            }
        }

        private void PruneLocked(DateTimeOffset observedAt)
        {
            DateTimeOffset cutoff =
                observedAt.ToUniversalTime() - _retention;
            LinkedListNode<AppliedUpdate> node = _pending.First;
            while (node != null)
            {
                LinkedListNode<AppliedUpdate> next = node.Next;
                if (node.Value.AppliedAt <= cutoff)
                {
                    _pending.Remove(node);
                    _expired++;
                }
                node = next;
            }
        }

        private static void Validate(
            ConfigurationAuthoritySource source,
            string semanticFingerprint,
            DateTimeOffset observedAt)
        {
            if (!Enum.IsDefined(
                    typeof(ConfigurationAuthoritySource),
                    source) ||
                !IsSha256Hex(semanticFingerprint))
            {
                throw new InvalidOperationException(
                    "Configuration overlap evidence is malformed.");
            }
            ValidateObservedAt(observedAt);
        }

        private static void ValidateObservedAt(DateTimeOffset observedAt)
        {
            if (observedAt == default(DateTimeOffset))
            {
                throw new InvalidOperationException(
                    "Configuration overlap evidence requires an observation time.");
            }
        }

        private static bool IsSha256Hex(string value)
        {
            if (value == null || value.Length != 64)
            {
                return false;
            }
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                bool digit = character >= '0' && character <= '9';
                bool uppercaseHex = character >= 'A' && character <= 'F';
                if (!digit && !uppercaseHex)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
