using System;
using System.Collections.Generic;

namespace NosGm.Authentication.Client.Configuration
{
    public enum ConfigurationUpdateParityVerdict
    {
        WaitingForTypedRuntime = 1,
        NoLiveObservations = 2,
        InProgress = 3,
        IncompleteEvidence = 4,
        OrderMismatch = 5,
        CountMismatch = 6,
        InvalidEvidence = 7,
        Parity = 8
    }

    public sealed class ConfigurationUpdateObservationLedgerSnapshot
    {
        private readonly IReadOnlyList<ConfigurationUpdateObservation>
            _observations;

        internal ConfigurationUpdateObservationLedgerSnapshot(
            string processGenerationId,
            long observedScs,
            long observedGrpc,
            long evictedObservations,
            ConfigurationUpdateObservation[] observations)
        {
            ProcessGenerationId = processGenerationId;
            ObservedScs = observedScs;
            ObservedGrpc = observedGrpc;
            EvictedObservations = evictedObservations;
            _observations = Array.AsReadOnly(
                observations ?? Array.Empty<ConfigurationUpdateObservation>());
        }

        public string ProcessGenerationId { get; }

        public long ObservedScs { get; }

        public long ObservedGrpc { get; }

        public long EvictedObservations { get; }

        public IReadOnlyList<ConfigurationUpdateObservation> Observations =>
            _observations;
    }

    public sealed class ConfigurationUpdateParityReport
    {
        internal ConfigurationUpdateParityReport(
            ConfigurationUpdateParityVerdict verdict,
            string processGenerationId,
            string runtimeGenerationId,
            ulong evaluatedThroughLedgerOrdinal,
            ulong windowStartLedgerOrdinal,
            int scsLiveCount,
            int grpcLiveCount,
            int matchedLiveCount,
            int grpcRecoveryCount,
            int grpcReplayCount,
            int ignoredScsBeforeWindow,
            long evictedObservations,
            int? firstMismatchIndex,
            ulong scsOrdinal,
            ulong grpcGeneration,
            long? oldestUnmatchedAgeMilliseconds,
            DateTimeOffset evaluatedAt)
        {
            Verdict = verdict;
            ProcessGenerationId = processGenerationId ?? string.Empty;
            RuntimeGenerationId = runtimeGenerationId ?? string.Empty;
            EvaluatedThroughLedgerOrdinal = evaluatedThroughLedgerOrdinal;
            WindowStartLedgerOrdinal = windowStartLedgerOrdinal;
            ScsLiveCount = scsLiveCount;
            GrpcLiveCount = grpcLiveCount;
            MatchedLiveCount = matchedLiveCount;
            GrpcRecoveryCount = grpcRecoveryCount;
            GrpcReplayCount = grpcReplayCount;
            IgnoredScsBeforeWindow = ignoredScsBeforeWindow;
            EvictedObservations = evictedObservations;
            FirstMismatchIndex = firstMismatchIndex;
            ScsOrdinal = scsOrdinal;
            GrpcGeneration = grpcGeneration;
            OldestUnmatchedAgeMilliseconds =
                oldestUnmatchedAgeMilliseconds;
            EvaluatedAt = evaluatedAt;
        }

        public ConfigurationUpdateParityVerdict Verdict { get; }

        public bool HasParity =>
            Verdict == ConfigurationUpdateParityVerdict.Parity;

        public bool HasTerminalMismatch =>
            Verdict == ConfigurationUpdateParityVerdict.IncompleteEvidence ||
            Verdict == ConfigurationUpdateParityVerdict.OrderMismatch ||
            Verdict == ConfigurationUpdateParityVerdict.CountMismatch ||
            Verdict == ConfigurationUpdateParityVerdict.InvalidEvidence;

        public string ProcessGenerationId { get; }

        public string RuntimeGenerationId { get; }

        public ulong EvaluatedThroughLedgerOrdinal { get; }

        public ulong WindowStartLedgerOrdinal { get; }

        public int ScsLiveCount { get; }

        public int GrpcLiveCount { get; }

        public int MatchedLiveCount { get; }

        public int GrpcRecoveryCount { get; }

        public int GrpcReplayCount { get; }

        public int IgnoredScsBeforeWindow { get; }

        public long EvictedObservations { get; }

        public int? FirstMismatchIndex { get; }

        public ulong ScsOrdinal { get; }

        public ulong GrpcGeneration { get; }

        public long? OldestUnmatchedAgeMilliseconds { get; }

        public DateTimeOffset EvaluatedAt { get; }
    }

    public static class ConfigurationUpdateParityComparator
    {
        public const int DefaultSettlementWindowMilliseconds = 5000;
        public const int MinimumSettlementWindowMilliseconds = 100;
        public const int MaximumSettlementWindowMilliseconds = 60000;

        public static ConfigurationUpdateParityReport Compare(
            ConfigurationUpdateObservationLedgerSnapshot snapshot,
            DateTimeOffset evaluatedAt,
            TimeSpan settlementWindow)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            ValidateSettlementWindow(settlementWindow);
            try
            {
                return CompareValidated(
                    snapshot,
                    evaluatedAt,
                    settlementWindow);
            }
            catch (InvalidOperationException)
            {
                return InvalidEvidence(snapshot, evaluatedAt);
            }
            catch (OverflowException)
            {
                return InvalidEvidence(snapshot, evaluatedAt);
            }
        }

        internal static TimeSpan NormalizeSettlementWindow(
            TimeSpan? settlementWindow)
        {
            TimeSpan normalized = settlementWindow ?? TimeSpan.FromMilliseconds(
                DefaultSettlementWindowMilliseconds);
            ValidateSettlementWindow(normalized);
            return normalized;
        }

        private static ConfigurationUpdateParityReport CompareValidated(
            ConfigurationUpdateObservationLedgerSnapshot snapshot,
            DateTimeOffset evaluatedAt,
            TimeSpan settlementWindow)
        {
            ValidateSnapshotHeader(snapshot);
            IReadOnlyList<ConfigurationUpdateObservation> observations =
                snapshot.Observations;
            long totalObserved = checked(
                snapshot.ObservedScs + snapshot.ObservedGrpc);
            if (totalObserved != checked(
                    snapshot.EvictedObservations + observations.Count))
            {
                throw new InvalidOperationException(
                    "Configuration parity evidence counters are inconsistent.");
            }

            string currentRuntimeGenerationId = string.Empty;
            ulong currentWindowStartLedgerOrdinal = 0;
            ulong previousLedgerOrdinal = 0;
            ulong previousScsOrdinal = 0;
            ulong previousGrpcOrdinal = 0;
            ulong previousGrpcGeneration = 0;
            bool retainedScs = false;
            bool retainedGrpc = false;
            var completedRuntimeGenerations = new HashSet<string>(
                StringComparer.Ordinal);

            for (int index = 0; index < observations.Count; index++)
            {
                ConfigurationUpdateObservation observation =
                    observations[index] ??
                    throw new InvalidOperationException(
                        "Configuration parity evidence contains a null observation.");
                ValidateCommonObservation(
                    snapshot.ProcessGenerationId,
                    observation,
                    previousLedgerOrdinal);
                previousLedgerOrdinal = observation.LedgerOrdinal;

                if (observation.Source ==
                    ConfigurationUpdateObservationSource.Scs)
                {
                    ValidateScsObservation(observation, previousScsOrdinal);
                    retainedScs = true;
                    previousScsOrdinal = observation.SourceOrdinal;
                    continue;
                }

                ValidateGrpcObservation(
                    observation,
                    previousGrpcOrdinal);
                retainedGrpc = true;
                previousGrpcOrdinal = observation.SourceOrdinal;
                if (!string.Equals(
                        currentRuntimeGenerationId,
                        observation.RuntimeGenerationId,
                        StringComparison.Ordinal))
                {
                    if (!string.IsNullOrEmpty(currentRuntimeGenerationId))
                    {
                        completedRuntimeGenerations.Add(
                            currentRuntimeGenerationId);
                    }
                    if (completedRuntimeGenerations.Contains(
                            observation.RuntimeGenerationId))
                    {
                        throw new InvalidOperationException(
                            "A Configuration runtime generation cannot reappear after replacement.");
                    }

                    currentRuntimeGenerationId =
                        observation.RuntimeGenerationId;
                    currentWindowStartLedgerOrdinal =
                        observation.LedgerOrdinal;
                    previousGrpcGeneration = 0;
                }

                if (observation.Generation <= previousGrpcGeneration)
                {
                    throw new InvalidOperationException(
                        "Configuration gRPC generations must remain strictly ordered inside one runtime.");
                }
                previousGrpcGeneration = observation.Generation;
            }

            ValidateRetainedBoundaries(
                snapshot,
                observations,
                totalObserved,
                retainedScs,
                previousScsOrdinal,
                retainedGrpc,
                previousGrpcOrdinal);

            ulong evaluatedThrough = checked((ulong)totalObserved);
            if (string.IsNullOrEmpty(currentRuntimeGenerationId))
            {
                return CreateReport(
                    ConfigurationUpdateParityVerdict.WaitingForTypedRuntime,
                    snapshot,
                    string.Empty,
                    evaluatedThrough,
                    0,
                    null,
                    null,
                    0,
                    0,
                    0,
                    null,
                    0,
                    0,
                    null,
                    evaluatedAt);
            }

            var scsLive = new List<ConfigurationUpdateObservation>();
            var grpcLive = new List<ConfigurationUpdateObservation>();
            int grpcRecoveryCount = 0;
            int grpcReplayCount = 0;
            int ignoredScsBeforeWindow = 0;
            for (int index = 0; index < observations.Count; index++)
            {
                ConfigurationUpdateObservation observation =
                    observations[index];
                if (observation.LedgerOrdinal <
                    currentWindowStartLedgerOrdinal)
                {
                    if (observation.Source ==
                        ConfigurationUpdateObservationSource.Scs)
                    {
                        ignoredScsBeforeWindow++;
                    }
                    continue;
                }

                if (observation.Source ==
                    ConfigurationUpdateObservationSource.Scs)
                {
                    scsLive.Add(observation);
                    continue;
                }

                if (!string.Equals(
                        observation.RuntimeGenerationId,
                        currentRuntimeGenerationId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Configuration parity window contains multiple typed runtimes.");
                }
                switch (observation.Phase)
                {
                    case ConfigurationUpdateObservationPhase.Live:
                        grpcLive.Add(observation);
                        break;
                    case ConfigurationUpdateObservationPhase.Replay:
                        grpcReplayCount++;
                        break;
                    case ConfigurationUpdateObservationPhase.Recovery:
                        grpcRecoveryCount++;
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Configuration parity evidence contains an invalid phase.");
                }
            }

            if (snapshot.EvictedObservations != 0)
            {
                return CreateReport(
                    ConfigurationUpdateParityVerdict.IncompleteEvidence,
                    snapshot,
                    currentRuntimeGenerationId,
                    evaluatedThrough,
                    currentWindowStartLedgerOrdinal,
                    scsLive,
                    grpcLive,
                    grpcRecoveryCount,
                    grpcReplayCount,
                    ignoredScsBeforeWindow,
                    null,
                    0,
                    0,
                    null,
                    evaluatedAt);
            }

            int comparableCount = Math.Min(scsLive.Count, grpcLive.Count);
            for (int index = 0; index < comparableCount; index++)
            {
                ConfigurationUpdateObservation scs = scsLive[index];
                ConfigurationUpdateObservation grpc = grpcLive[index];
                if (!string.Equals(
                        scs.SemanticFingerprint,
                        grpc.SemanticFingerprint,
                        StringComparison.Ordinal))
                {
                    return CreateReport(
                        ConfigurationUpdateParityVerdict.OrderMismatch,
                        snapshot,
                        currentRuntimeGenerationId,
                        evaluatedThrough,
                        currentWindowStartLedgerOrdinal,
                        scsLive,
                        grpcLive,
                        grpcRecoveryCount,
                        grpcReplayCount,
                        ignoredScsBeforeWindow,
                        index,
                        scs.SourceOrdinal,
                        grpc.Generation,
                        null,
                        evaluatedAt);
                }
            }

            if (scsLive.Count == 0 && grpcLive.Count == 0)
            {
                return CreateReport(
                    ConfigurationUpdateParityVerdict.NoLiveObservations,
                    snapshot,
                    currentRuntimeGenerationId,
                    evaluatedThrough,
                    currentWindowStartLedgerOrdinal,
                    scsLive,
                    grpcLive,
                    grpcRecoveryCount,
                    grpcReplayCount,
                    ignoredScsBeforeWindow,
                    null,
                    0,
                    0,
                    null,
                    evaluatedAt);
            }

            if (scsLive.Count == grpcLive.Count)
            {
                return CreateReport(
                    ConfigurationUpdateParityVerdict.Parity,
                    snapshot,
                    currentRuntimeGenerationId,
                    evaluatedThrough,
                    currentWindowStartLedgerOrdinal,
                    scsLive,
                    grpcLive,
                    grpcRecoveryCount,
                    grpcReplayCount,
                    ignoredScsBeforeWindow,
                    null,
                    0,
                    0,
                    null,
                    evaluatedAt);
            }

            ConfigurationUpdateObservation oldestUnmatched =
                scsLive.Count > grpcLive.Count
                    ? scsLive[comparableCount]
                    : grpcLive[comparableCount];
            long unmatchedAgeMilliseconds = ToNonNegativeMilliseconds(
                evaluatedAt - oldestUnmatched.ObservedAt);
            ConfigurationUpdateParityVerdict pendingVerdict =
                unmatchedAgeMilliseconds <= settlementWindow.TotalMilliseconds
                    ? ConfigurationUpdateParityVerdict.InProgress
                    : ConfigurationUpdateParityVerdict.CountMismatch;
            return CreateReport(
                pendingVerdict,
                snapshot,
                currentRuntimeGenerationId,
                evaluatedThrough,
                currentWindowStartLedgerOrdinal,
                scsLive,
                grpcLive,
                grpcRecoveryCount,
                grpcReplayCount,
                ignoredScsBeforeWindow,
                null,
                oldestUnmatched.Source ==
                    ConfigurationUpdateObservationSource.Scs
                    ? oldestUnmatched.SourceOrdinal
                    : 0,
                oldestUnmatched.Source ==
                    ConfigurationUpdateObservationSource.Grpc
                    ? oldestUnmatched.Generation
                    : 0,
                unmatchedAgeMilliseconds,
                evaluatedAt);
        }

        private static ConfigurationUpdateParityReport CreateReport(
            ConfigurationUpdateParityVerdict verdict,
            ConfigurationUpdateObservationLedgerSnapshot snapshot,
            string runtimeGenerationId,
            ulong evaluatedThrough,
            ulong windowStart,
            IReadOnlyList<ConfigurationUpdateObservation> scsLive,
            IReadOnlyList<ConfigurationUpdateObservation> grpcLive,
            int grpcRecoveryCount,
            int grpcReplayCount,
            int ignoredScsBeforeWindow,
            int? firstMismatchIndex,
            ulong scsOrdinal,
            ulong grpcGeneration,
            long? oldestUnmatchedAgeMilliseconds,
            DateTimeOffset evaluatedAt)
        {
            int scsCount = scsLive?.Count ?? 0;
            int grpcCount = grpcLive?.Count ?? 0;
            int matched = firstMismatchIndex ?? Math.Min(scsCount, grpcCount);
            return new ConfigurationUpdateParityReport(
                verdict,
                snapshot.ProcessGenerationId,
                runtimeGenerationId,
                evaluatedThrough,
                windowStart,
                scsCount,
                grpcCount,
                matched,
                grpcRecoveryCount,
                grpcReplayCount,
                ignoredScsBeforeWindow,
                snapshot.EvictedObservations,
                firstMismatchIndex,
                scsOrdinal,
                grpcGeneration,
                oldestUnmatchedAgeMilliseconds,
                evaluatedAt);
        }

        private static ConfigurationUpdateParityReport InvalidEvidence(
            ConfigurationUpdateObservationLedgerSnapshot snapshot,
            DateTimeOffset evaluatedAt)
        {
            return new ConfigurationUpdateParityReport(
                ConfigurationUpdateParityVerdict.InvalidEvidence,
                snapshot.ProcessGenerationId,
                string.Empty,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                snapshot.EvictedObservations,
                null,
                0,
                0,
                null,
                evaluatedAt);
        }

        private static void ValidateSnapshotHeader(
            ConfigurationUpdateObservationLedgerSnapshot snapshot)
        {
            if (!IsCanonicalNonEmptyGuid(snapshot.ProcessGenerationId) ||
                snapshot.ObservedScs < 0 ||
                snapshot.ObservedGrpc < 0 ||
                snapshot.EvictedObservations < 0 ||
                snapshot.Observations == null ||
                snapshot.Observations.Count >
                    ConfigurationUpdateObservationLedger
                        .MaximumObservationCapacity)
            {
                throw new InvalidOperationException(
                    "Configuration parity snapshot is malformed.");
            }
        }

        private static void ValidateCommonObservation(
            string processGenerationId,
            ConfigurationUpdateObservation observation,
            ulong previousLedgerOrdinal)
        {
            if (!string.Equals(
                    processGenerationId,
                    observation.ProcessGenerationId,
                    StringComparison.Ordinal) ||
                observation.LedgerOrdinal <= previousLedgerOrdinal ||
                observation.SourceOrdinal == 0 ||
                !Enum.IsDefined(
                    typeof(ConfigurationUpdateObservationSource),
                    observation.Source) ||
                !Enum.IsDefined(
                    typeof(ConfigurationUpdateObservationPhase),
                    observation.Phase) ||
                observation.MaxGold <= 0 ||
                !IsSha256Hex(observation.SemanticFingerprint))
            {
                throw new InvalidOperationException(
                    "Configuration parity observation is malformed.");
            }
        }

        private static void ValidateScsObservation(
            ConfigurationUpdateObservation observation,
            ulong previousScsOrdinal)
        {
            if (observation.SourceOrdinal <= previousScsOrdinal ||
                observation.Phase != ConfigurationUpdateObservationPhase.Live ||
                !string.IsNullOrEmpty(observation.RuntimeGenerationId) ||
                observation.Generation != 0)
            {
                throw new InvalidOperationException(
                    "Configuration SCS parity observation is malformed.");
            }
        }

        private static void ValidateGrpcObservation(
            ConfigurationUpdateObservation observation,
            ulong previousGrpcOrdinal)
        {
            if (observation.SourceOrdinal <= previousGrpcOrdinal ||
                !IsCanonicalNonEmptyGuid(
                    observation.RuntimeGenerationId) ||
                observation.Generation == 0)
            {
                throw new InvalidOperationException(
                    "Configuration gRPC parity observation is malformed.");
            }
        }

        private static void ValidateRetainedBoundaries(
            ConfigurationUpdateObservationLedgerSnapshot snapshot,
            IReadOnlyList<ConfigurationUpdateObservation> observations,
            long totalObserved,
            bool retainedScs,
            ulong previousScsOrdinal,
            bool retainedGrpc,
            ulong previousGrpcOrdinal)
        {
            if (observations.Count != 0)
            {
                ulong expectedFirst = checked(
                    (ulong)snapshot.EvictedObservations + 1UL);
                if (observations[0].LedgerOrdinal != expectedFirst ||
                    observations[observations.Count - 1].LedgerOrdinal !=
                        checked((ulong)totalObserved))
                {
                    throw new InvalidOperationException(
                        "Configuration parity ledger boundaries are inconsistent.");
                }
            }
            if ((retainedScs && previousScsOrdinal !=
                    checked((ulong)snapshot.ObservedScs)) ||
                (retainedGrpc && previousGrpcOrdinal !=
                    checked((ulong)snapshot.ObservedGrpc)))
            {
                throw new InvalidOperationException(
                    "Configuration parity source boundaries are inconsistent.");
            }
        }

        private static long ToNonNegativeMilliseconds(TimeSpan age)
        {
            if (age <= TimeSpan.Zero)
            {
                return 0;
            }
            if (age.TotalMilliseconds >= long.MaxValue)
            {
                return long.MaxValue;
            }
            return checked((long)age.TotalMilliseconds);
        }

        private static void ValidateSettlementWindow(
            TimeSpan settlementWindow)
        {
            if (settlementWindow.TotalMilliseconds <
                    MinimumSettlementWindowMilliseconds ||
                settlementWindow.TotalMilliseconds >
                    MaximumSettlementWindowMilliseconds)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settlementWindow),
                    "Configuration parity settlement must be between " +
                    MinimumSettlementWindowMilliseconds + " and " +
                    MaximumSettlementWindowMilliseconds + " milliseconds.");
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
