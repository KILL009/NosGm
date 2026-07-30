using NosGm.Communication.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Master.Library.Client
{
    public sealed class CommunicationCallbackScsObservation
    {
        internal CommunicationCallbackScsObservation(
            string processIdentity,
            string runtimeGenerationId,
            ulong localOrdinal,
            WireV1.CommunicationCallbackKind kind,
            string semanticFingerprint,
            DateTimeOffset observedAt)
        {
            ProcessIdentity = processIdentity;
            RuntimeGenerationId = runtimeGenerationId;
            LocalOrdinal = localOrdinal;
            Kind = kind;
            SemanticFingerprint = semanticFingerprint;
            ObservedAt = observedAt;
        }

        public string ProcessIdentity { get; }

        public string RuntimeGenerationId { get; }

        public ulong LocalOrdinal { get; }

        public WireV1.CommunicationCallbackKind Kind { get; }

        public string SemanticFingerprint { get; }

        public DateTimeOffset ObservedAt { get; }
    }

    public sealed class CommunicationCallbackScsObservationLedger
    {
        public const int DefaultObservationCapacity = 4096;
        public const int MaximumObservationCapacity = 16384;

        private static readonly Lazy<CommunicationCallbackScsObservationLedger>
            LazyInstance =
                new Lazy<CommunicationCallbackScsObservationLedger>(
                    () => new CommunicationCallbackScsObservationLedger());

        private readonly int _capacity;
        private readonly Queue<CommunicationCallbackScsObservation>
            _observations;
        private readonly object _syncRoot = new object();
        private string _processIdentity = string.Empty;
        private string _runtimeGenerationId = string.Empty;
        private long _localOrdinal;
        private long _observedCallbacks;
        private long _evictedObservations;
        private bool _windowActive;

        public CommunicationCallbackScsObservationLedger(
            int observationCapacity = DefaultObservationCapacity)
        {
            if (observationCapacity <= 0 ||
                observationCapacity > MaximumObservationCapacity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(observationCapacity),
                    "SCS observation capacity must be between 1 and " +
                    MaximumObservationCapacity + ".");
            }

            _capacity = observationCapacity;
            _observations =
                new Queue<CommunicationCallbackScsObservation>(
                    observationCapacity);
        }

        public static CommunicationCallbackScsObservationLedger Instance =>
            LazyInstance.Value;

        public int ObservationCapacity => _capacity;

        public bool IsWindowActive
        {
            get
            {
                lock (_syncRoot)
                {
                    return _windowActive;
                }
            }
        }

        public long ObservedCallbacks =>
            Interlocked.Read(ref _observedCallbacks);

        public long EvictedObservations =>
            Interlocked.Read(ref _evictedObservations);

        public void BeginWindow(
            string processIdentity,
            CommunicationCallbackReplayEvidence evidence)
        {
            if (string.IsNullOrWhiteSpace(processIdentity) ||
                !string.Equals(
                    processIdentity,
                    processIdentity.Trim(),
                    StringComparison.Ordinal) ||
                evidence == null ||
                !IsCanonicalNonEmptyGuid(evidence.RuntimeGenerationId))
            {
                throw new InvalidOperationException(
                    "The SCS observation window identity is invalid.");
            }

            lock (_syncRoot)
            {
                _observations.Clear();
                _processIdentity = processIdentity;
                _runtimeGenerationId = evidence.RuntimeGenerationId;
                Interlocked.Exchange(ref _localOrdinal, 0);
                Interlocked.Exchange(ref _observedCallbacks, 0);
                Interlocked.Exchange(ref _evictedObservations, 0);
                _windowActive = true;
            }
        }

        public void EndWindow()
        {
            lock (_syncRoot)
            {
                _windowActive = false;
            }
        }

        public bool TryRecord(
            WireV1.CommunicationCallbackKind kind,
            string semanticFingerprint)
        {
            if (kind == WireV1.CommunicationCallbackKind.Unspecified ||
                !IsSha256Hex(semanticFingerprint))
            {
                throw new InvalidOperationException(
                    "The SCS callback observation is malformed.");
            }

            lock (_syncRoot)
            {
                if (!_windowActive)
                {
                    return false;
                }

                long next = checked(_localOrdinal + 1);
                _localOrdinal = next;
                if (_observations.Count == _capacity)
                {
                    _observations.Dequeue();
                    Interlocked.Increment(ref _evictedObservations);
                }
                _observations.Enqueue(
                    new CommunicationCallbackScsObservation(
                        _processIdentity,
                        _runtimeGenerationId,
                        checked((ulong)next),
                        kind,
                        semanticFingerprint,
                        DateTimeOffset.UtcNow));
                Interlocked.Increment(ref _observedCallbacks);
                return true;
            }
        }

        public IReadOnlyList<CommunicationCallbackScsObservation>
            GetObservationSnapshot()
        {
            lock (_syncRoot)
            {
                return _observations.ToArray();
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
                bool upperHex = character >= 'A' && character <= 'F';
                if (!digit && !upperHex)
                {
                    return false;
                }
            }
            return true;
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
