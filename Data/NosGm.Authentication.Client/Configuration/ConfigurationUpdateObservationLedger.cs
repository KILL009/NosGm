using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace NosGm.Authentication.Client.Configuration
{
    public enum ConfigurationUpdateObservationSource
    {
        Scs = 1,
        Grpc = 2
    }

    public enum ConfigurationUpdateObservationPhase
    {
        Live = 1,
        Replay = 2,
        Recovery = 3
    }

    public sealed class ConfigurationUpdateObservation
    {
        internal ConfigurationUpdateObservation(
            string processGenerationId,
            ulong ledgerOrdinal,
            ulong sourceOrdinal,
            ConfigurationUpdateObservationSource source,
            ConfigurationUpdateObservationPhase phase,
            string runtimeGenerationId,
            ulong generation,
            ConfigurationTransportSnapshot snapshot,
            string semanticFingerprint,
            DateTimeOffset observedAt)
        {
            ProcessGenerationId = processGenerationId;
            LedgerOrdinal = ledgerOrdinal;
            SourceOrdinal = sourceOrdinal;
            Source = source;
            Phase = phase;
            RuntimeGenerationId = runtimeGenerationId;
            Generation = generation;
            MaxGold = snapshot.MaxGold;
            TimeExpBuffUnixTimeMilliseconds =
                snapshot.TimeExpBuffUnixTimeMilliseconds;
            TimeGoldBuffUnixTimeMilliseconds =
                snapshot.TimeGoldBuffUnixTimeMilliseconds;
            SemanticFingerprint = semanticFingerprint;
            ObservedAt = observedAt;
        }

        public string ProcessGenerationId { get; }

        public ulong LedgerOrdinal { get; }

        public ulong SourceOrdinal { get; }

        public ConfigurationUpdateObservationSource Source { get; }

        public ConfigurationUpdateObservationPhase Phase { get; }

        public string RuntimeGenerationId { get; }

        public ulong Generation { get; }

        public long MaxGold { get; }

        public long TimeExpBuffUnixTimeMilliseconds { get; }

        public long TimeGoldBuffUnixTimeMilliseconds { get; }

        public string SemanticFingerprint { get; }

        public DateTimeOffset ObservedAt { get; }
    }

    public static class ConfigurationSnapshotSemanticFingerprint
    {
        public static string Compute(ConfigurationTransportSnapshot snapshot)
        {
            Validate(snapshot);
            string canonical =
                snapshot.MaxGold.ToString(CultureInfo.InvariantCulture) + "\n" +
                snapshot.TimeExpBuffUnixTimeMilliseconds.ToString(
                    CultureInfo.InvariantCulture) + "\n" +
                snapshot.TimeGoldBuffUnixTimeMilliseconds.ToString(
                    CultureInfo.InvariantCulture);
            byte[] payload = Encoding.UTF8.GetBytes(canonical);
            byte[] digest;
            using (SHA256 sha256 = SHA256.Create())
            {
                digest = sha256.ComputeHash(payload);
            }

            var result = new StringBuilder(digest.Length * 2);
            for (int index = 0; index < digest.Length; index++)
            {
                result.Append(digest[index].ToString("X2", CultureInfo.InvariantCulture));
            }
            return result.ToString();
        }

        internal static void Validate(ConfigurationTransportSnapshot snapshot)
        {
            if (snapshot == null || snapshot.MaxGold <= 0)
            {
                throw new InvalidOperationException(
                    "The Configuration observation snapshot is malformed.");
            }
        }
    }

    public sealed class ConfigurationUpdateObservationLedger
    {
        public const int DefaultObservationCapacity = 512;
        public const int MaximumObservationCapacity = 4096;

        private static readonly Lazy<ConfigurationUpdateObservationLedger>
            LazyInstance =
                new Lazy<ConfigurationUpdateObservationLedger>(
                    () => new ConfigurationUpdateObservationLedger());

        private readonly int _capacity;
        private readonly Queue<ConfigurationUpdateObservation> _observations;
        private readonly string _processGenerationId =
            Guid.NewGuid().ToString("D");
        private readonly object _syncRoot = new object();
        private long _evictedObservations;
        private long _grpcOrdinal;
        private long _ledgerOrdinal;
        private long _observedGrpc;
        private long _observedScs;
        private long _scsOrdinal;

        public ConfigurationUpdateObservationLedger(
            int observationCapacity = DefaultObservationCapacity)
        {
            if (observationCapacity <= 0 ||
                observationCapacity > MaximumObservationCapacity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(observationCapacity),
                    "Configuration observation capacity must be between 1 and " +
                    MaximumObservationCapacity + ".");
            }

            _capacity = observationCapacity;
            _observations =
                new Queue<ConfigurationUpdateObservation>(observationCapacity);
        }

        public static ConfigurationUpdateObservationLedger Instance =>
            LazyInstance.Value;

        public int ObservationCapacity => _capacity;

        public string ProcessGenerationId => _processGenerationId;

        public long ObservedScs => Interlocked.Read(ref _observedScs);

        public long ObservedGrpc => Interlocked.Read(ref _observedGrpc);

        public long EvictedObservations =>
            Interlocked.Read(ref _evictedObservations);

        public ConfigurationUpdateObservation RecordScs(
            ConfigurationTransportSnapshot snapshot)
        {
            return Record(
                ConfigurationUpdateObservationSource.Scs,
                ConfigurationUpdateObservationPhase.Live,
                string.Empty,
                0,
                snapshot);
        }

        public ConfigurationUpdateObservation RecordGrpc(
            ConfigurationTransportUpdate update)
        {
            if (update == null ||
                update.Configuration == null ||
                update.Generation == 0 ||
                !IsCanonicalNonEmptyGuid(update.RuntimeGenerationId) ||
                (update.Replayed && update.RecoveredFromSnapshot))
            {
                throw new InvalidOperationException(
                    "The typed Configuration observation is malformed.");
            }

            ConfigurationUpdateObservationPhase phase =
                update.RecoveredFromSnapshot
                    ? ConfigurationUpdateObservationPhase.Recovery
                    : update.Replayed
                        ? ConfigurationUpdateObservationPhase.Replay
                        : ConfigurationUpdateObservationPhase.Live;
            return Record(
                ConfigurationUpdateObservationSource.Grpc,
                phase,
                update.RuntimeGenerationId,
                update.Generation,
                update.Configuration);
        }

        public IReadOnlyList<ConfigurationUpdateObservation>
            GetObservationSnapshot()
        {
            lock (_syncRoot)
            {
                return _observations.ToArray();
            }
        }

        private ConfigurationUpdateObservation Record(
            ConfigurationUpdateObservationSource source,
            ConfigurationUpdateObservationPhase phase,
            string runtimeGenerationId,
            ulong generation,
            ConfigurationTransportSnapshot snapshot)
        {
            ConfigurationSnapshotSemanticFingerprint.Validate(snapshot);
            string fingerprint =
                ConfigurationSnapshotSemanticFingerprint.Compute(snapshot);
            lock (_syncRoot)
            {
                long nextLedgerOrdinal = checked(_ledgerOrdinal + 1);
                _ledgerOrdinal = nextLedgerOrdinal;
                long nextSourceOrdinal;
                if (source == ConfigurationUpdateObservationSource.Scs)
                {
                    nextSourceOrdinal = checked(_scsOrdinal + 1);
                    _scsOrdinal = nextSourceOrdinal;
                }
                else
                {
                    nextSourceOrdinal = checked(_grpcOrdinal + 1);
                    _grpcOrdinal = nextSourceOrdinal;
                }

                if (_observations.Count == _capacity)
                {
                    _observations.Dequeue();
                    Interlocked.Increment(ref _evictedObservations);
                }

                var observation = new ConfigurationUpdateObservation(
                    _processGenerationId,
                    checked((ulong)nextLedgerOrdinal),
                    checked((ulong)nextSourceOrdinal),
                    source,
                    phase,
                    runtimeGenerationId,
                    generation,
                    snapshot,
                    fingerprint,
                    DateTimeOffset.UtcNow);
                _observations.Enqueue(observation);
                if (source == ConfigurationUpdateObservationSource.Scs)
                {
                    Interlocked.Increment(ref _observedScs);
                }
                else
                {
                    Interlocked.Increment(ref _observedGrpc);
                }
                return observation;
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
