using System;
using System.Collections.Generic;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Communication.Client
{
    public enum CommunicationCallbackParitySource
    {
        TypedGrpc = 1,
        LegacyScs = 2
    }

    public enum CommunicationCallbackParityVerdict
    {
        InProgress = 1,
        ReplayIncomplete = 2,
        IdentityMismatch = 3,
        GenerationMismatch = 4,
        ReplayBoundaryMismatch = 5,
        IncompleteEvidence = 6,
        NoLiveObservations = 7,
        CountMismatch = 8,
        OrderMismatch = 9,
        InvalidEvidence = 10,
        Parity = 11
    }

    public sealed class CommunicationCallbackParitySample
    {
        public CommunicationCallbackParitySample(
            string runtimeGenerationId,
            ulong sourceOrdinal,
            WireV1.CommunicationCallbackKind kind,
            string semanticFingerprint)
        {
            if (!IsCanonicalNonEmptyGuid(runtimeGenerationId) ||
                sourceOrdinal == 0 ||
                sourceOrdinal > (ulong)long.MaxValue ||
                kind == WireV1.CommunicationCallbackKind.Unspecified ||
                !Enum.IsDefined(
                    typeof(WireV1.CommunicationCallbackKind),
                    kind) ||
                !IsSha256Hex(semanticFingerprint))
            {
                throw new InvalidOperationException(
                    "The callback parity sample is malformed.");
            }

            RuntimeGenerationId = runtimeGenerationId;
            SourceOrdinal = sourceOrdinal;
            Kind = kind;
            SemanticFingerprint = semanticFingerprint;
        }

        public string RuntimeGenerationId { get; }

        public ulong SourceOrdinal { get; }

        public WireV1.CommunicationCallbackKind Kind { get; }

        public string SemanticFingerprint { get; }

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

    public sealed class CommunicationCallbackParityWindow
    {
        private readonly CommunicationCallbackParitySample[] _liveSamples;

        public CommunicationCallbackParityWindow(
            CommunicationCallbackParitySource source,
            string processIdentity,
            string runtimeGenerationId,
            bool isActive,
            CommunicationCallbackReplayEvidence replayEvidence,
            long observedCallbacks,
            long evictedObservations,
            IReadOnlyList<CommunicationCallbackParitySample> liveSamples)
        {
            if (!Enum.IsDefined(
                    typeof(CommunicationCallbackParitySource),
                    source) ||
                string.IsNullOrWhiteSpace(processIdentity) ||
                processIdentity.Length > 128 ||
                !string.Equals(
                    processIdentity,
                    processIdentity.Trim(),
                    StringComparison.Ordinal) ||
                observedCallbacks < 0 ||
                evictedObservations < 0 ||
                liveSamples == null)
            {
                throw new InvalidOperationException(
                    "The callback parity window is malformed.");
            }

            if (!string.IsNullOrEmpty(runtimeGenerationId) &&
                !IsCanonicalNonEmptyGuid(runtimeGenerationId))
            {
                throw new InvalidOperationException(
                    "The callback parity generation is malformed.");
            }

            if (replayEvidence != null &&
                (!string.Equals(
                    replayEvidence.RuntimeGenerationId,
                    runtimeGenerationId,
                    StringComparison.Ordinal) ||
                 replayEvidence.ReplayThroughSequence >
                     (ulong)long.MaxValue ||
                 replayEvidence.ResumeAfterSequence >
                     replayEvidence.ReplayThroughSequence))
            {
                throw new InvalidOperationException(
                    "The callback parity replay evidence is malformed.");
            }

            _liveSamples =
                new CommunicationCallbackParitySample[liveSamples.Count];
            ulong previousOrdinal = 0;
            for (int index = 0; index < liveSamples.Count; index++)
            {
                CommunicationCallbackParitySample sample =
                    liveSamples[index] ??
                    throw new InvalidOperationException(
                        "The callback parity window contains a null sample.");
                if (!string.Equals(
                        sample.RuntimeGenerationId,
                        runtimeGenerationId,
                        StringComparison.Ordinal) ||
                    sample.SourceOrdinal <= previousOrdinal)
                {
                    throw new InvalidOperationException(
                        "Callback parity samples must belong to one generation and remain strictly ordered.");
                }

                previousOrdinal = sample.SourceOrdinal;
                _liveSamples[index] = sample;
            }

            if (observedCallbacks < _liveSamples.Length)
            {
                throw new InvalidOperationException(
                    "The callback parity sample count exceeds the observed callback count.");
            }

            Source = source;
            ProcessIdentity = processIdentity;
            RuntimeGenerationId = runtimeGenerationId;
            IsActive = isActive;
            ReplayEvidence = replayEvidence;
            ObservedCallbacks = observedCallbacks;
            EvictedObservations = evictedObservations;
        }

        public CommunicationCallbackParitySource Source { get; }

        public string ProcessIdentity { get; }

        public string RuntimeGenerationId { get; }

        public bool IsActive { get; }

        public CommunicationCallbackReplayEvidence ReplayEvidence { get; }

        public long ObservedCallbacks { get; }

        public long EvictedObservations { get; }

        public IReadOnlyList<CommunicationCallbackParitySample> LiveSamples =>
            Array.AsReadOnly(_liveSamples);

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

    public sealed class CommunicationCallbackParityReport
    {
        internal CommunicationCallbackParityReport(
            CommunicationCallbackParityVerdict verdict,
            string processIdentity,
            string runtimeGenerationId,
            int typedLiveCount,
            int scsLiveCount,
            long typedEvictions,
            long scsEvictions,
            int? firstMismatchIndex,
            ulong typedSequence,
            ulong scsOrdinal)
        {
            Verdict = verdict;
            ProcessIdentity = processIdentity ?? string.Empty;
            RuntimeGenerationId = runtimeGenerationId ?? string.Empty;
            TypedLiveCount = typedLiveCount;
            ScsLiveCount = scsLiveCount;
            TypedEvictions = typedEvictions;
            ScsEvictions = scsEvictions;
            FirstMismatchIndex = firstMismatchIndex;
            TypedSequence = typedSequence;
            ScsOrdinal = scsOrdinal;
        }

        public CommunicationCallbackParityVerdict Verdict { get; }

        public bool HasParity =>
            Verdict == CommunicationCallbackParityVerdict.Parity;

        public string ProcessIdentity { get; }

        public string RuntimeGenerationId { get; }

        public int TypedLiveCount { get; }

        public int ScsLiveCount { get; }

        public long TypedEvictions { get; }

        public long ScsEvictions { get; }

        public int? FirstMismatchIndex { get; }

        public ulong TypedSequence { get; }

        public ulong ScsOrdinal { get; }

        public static CommunicationCallbackParityReport InProgress(
            string processIdentity,
            string runtimeGenerationId)
        {
            return new CommunicationCallbackParityReport(
                CommunicationCallbackParityVerdict.InProgress,
                processIdentity,
                runtimeGenerationId,
                0,
                0,
                0,
                0,
                null,
                0,
                0);
        }

        public static CommunicationCallbackParityReport InvalidEvidence(
            string processIdentity)
        {
            return new CommunicationCallbackParityReport(
                CommunicationCallbackParityVerdict.InvalidEvidence,
                processIdentity,
                string.Empty,
                0,
                0,
                0,
                0,
                null,
                0,
                0);
        }
    }

    public static class CommunicationCallbackParityComparator
    {
        public static CommunicationCallbackParityReport Compare(
            CommunicationCallbackParityWindow typedWindow,
            CommunicationCallbackParityWindow scsWindow)
        {
            if (typedWindow == null)
            {
                throw new ArgumentNullException(nameof(typedWindow));
            }
            if (scsWindow == null)
            {
                throw new ArgumentNullException(nameof(scsWindow));
            }
            if (typedWindow.Source !=
                    CommunicationCallbackParitySource.TypedGrpc ||
                scsWindow.Source !=
                    CommunicationCallbackParitySource.LegacyScs)
            {
                throw new InvalidOperationException(
                    "Callback parity requires one typed gRPC window and one legacy SCS window.");
            }

            if (!string.Equals(
                    typedWindow.ProcessIdentity,
                    scsWindow.ProcessIdentity,
                    StringComparison.Ordinal))
            {
                return CreateReport(
                    CommunicationCallbackParityVerdict.IdentityMismatch,
                    typedWindow,
                    scsWindow);
            }

            if (typedWindow.IsActive || scsWindow.IsActive)
            {
                return CreateReport(
                    CommunicationCallbackParityVerdict.InProgress,
                    typedWindow,
                    scsWindow);
            }

            if (typedWindow.ReplayEvidence == null ||
                scsWindow.ReplayEvidence == null)
            {
                return CreateReport(
                    CommunicationCallbackParityVerdict.ReplayIncomplete,
                    typedWindow,
                    scsWindow);
            }

            if (!string.Equals(
                    typedWindow.RuntimeGenerationId,
                    scsWindow.RuntimeGenerationId,
                    StringComparison.Ordinal))
            {
                return CreateReport(
                    CommunicationCallbackParityVerdict.GenerationMismatch,
                    typedWindow,
                    scsWindow);
            }

            if (!HasSameReplayBoundary(
                    typedWindow.ReplayEvidence,
                    scsWindow.ReplayEvidence))
            {
                return CreateReport(
                    CommunicationCallbackParityVerdict.ReplayBoundaryMismatch,
                    typedWindow,
                    scsWindow);
            }

            if (typedWindow.EvictedObservations != 0 ||
                scsWindow.EvictedObservations != 0)
            {
                return CreateReport(
                    CommunicationCallbackParityVerdict.IncompleteEvidence,
                    typedWindow,
                    scsWindow);
            }

            IReadOnlyList<CommunicationCallbackParitySample> typedSamples =
                typedWindow.LiveSamples;
            IReadOnlyList<CommunicationCallbackParitySample> scsSamples =
                scsWindow.LiveSamples;
            if (typedSamples.Count == 0 && scsSamples.Count == 0)
            {
                return CreateReport(
                    CommunicationCallbackParityVerdict.NoLiveObservations,
                    typedWindow,
                    scsWindow);
            }
            if (typedSamples.Count != scsSamples.Count)
            {
                return CreateReport(
                    CommunicationCallbackParityVerdict.CountMismatch,
                    typedWindow,
                    scsWindow);
            }

            for (int index = 0; index < typedSamples.Count; index++)
            {
                CommunicationCallbackParitySample typed = typedSamples[index];
                CommunicationCallbackParitySample scs = scsSamples[index];
                if (typed.Kind != scs.Kind ||
                    !string.Equals(
                        typed.SemanticFingerprint,
                        scs.SemanticFingerprint,
                        StringComparison.Ordinal))
                {
                    return CreateReport(
                        CommunicationCallbackParityVerdict.OrderMismatch,
                        typedWindow,
                        scsWindow,
                        index,
                        typed.SourceOrdinal,
                        scs.SourceOrdinal);
                }
            }

            return CreateReport(
                CommunicationCallbackParityVerdict.Parity,
                typedWindow,
                scsWindow);
        }

        private static bool HasSameReplayBoundary(
            CommunicationCallbackReplayEvidence typed,
            CommunicationCallbackReplayEvidence scs)
        {
            return string.Equals(
                       typed.RuntimeGenerationId,
                       scs.RuntimeGenerationId,
                       StringComparison.Ordinal) &&
                   typed.ReplayThroughSequence ==
                       scs.ReplayThroughSequence &&
                   typed.ResumeAfterSequence ==
                       scs.ResumeAfterSequence &&
                   typed.ReplayedEvents == scs.ReplayedEvents;
        }

        private static CommunicationCallbackParityReport CreateReport(
            CommunicationCallbackParityVerdict verdict,
            CommunicationCallbackParityWindow typed,
            CommunicationCallbackParityWindow scs,
            int? firstMismatchIndex = null,
            ulong typedSequence = 0,
            ulong scsOrdinal = 0)
        {
            string generation = string.Equals(
                    typed.RuntimeGenerationId,
                    scs.RuntimeGenerationId,
                    StringComparison.Ordinal)
                ? typed.RuntimeGenerationId
                : string.Empty;
            return new CommunicationCallbackParityReport(
                verdict,
                typed.ProcessIdentity,
                generation,
                typed.LiveSamples.Count,
                scs.LiveSamples.Count,
                typed.EvictedObservations,
                scs.EvictedObservations,
                firstMismatchIndex,
                typedSequence,
                scsOrdinal);
        }
    }
}
