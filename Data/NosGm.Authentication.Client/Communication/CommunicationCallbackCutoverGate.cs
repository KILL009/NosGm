using System;
using System.Collections.Generic;
using System.Threading;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Communication.Client
{
    public enum CommunicationCallbackCutoverState
    {
        ScsAuthoritative = 1,
        Armed = 2,
        TypedGrpcAuthoritative = 3,
        RolledBack = 4
    }

    public sealed class CommunicationCallbackKindParityEvidence
    {
        public CommunicationCallbackKindParityEvidence(
            string processIdentity,
            WireV1.CommunicationCallbackKind kind,
            string runtimeGenerationId,
            CommunicationCallbackParityVerdict verdict,
            int typedLiveCount,
            int scsLiveCount,
            DateTimeOffset observedAt)
        {
            if (!IsValidIdentity(processIdentity) ||
                !IsSupportedKind(kind) ||
                (!string.IsNullOrEmpty(runtimeGenerationId) &&
                 !IsCanonicalNonEmptyGuid(runtimeGenerationId)) ||
                !Enum.IsDefined(
                    typeof(CommunicationCallbackParityVerdict),
                    verdict) ||
                typedLiveCount < 0 ||
                scsLiveCount < 0 ||
                observedAt == default(DateTimeOffset))
            {
                throw new InvalidOperationException(
                    "The callback kind parity evidence is malformed.");
            }

            if (verdict == CommunicationCallbackParityVerdict.Parity &&
                (!IsCanonicalNonEmptyGuid(runtimeGenerationId) ||
                 typedLiveCount == 0 ||
                 typedLiveCount != scsLiveCount))
            {
                throw new InvalidOperationException(
                    "Positive callback kind parity requires equal non-empty live counts.");
            }

            ProcessIdentity = processIdentity;
            Kind = kind;
            RuntimeGenerationId = runtimeGenerationId;
            Verdict = verdict;
            TypedLiveCount = typedLiveCount;
            ScsLiveCount = scsLiveCount;
            ObservedAt = observedAt.ToUniversalTime();
        }

        public string ProcessIdentity { get; }

        public WireV1.CommunicationCallbackKind Kind { get; }

        public string RuntimeGenerationId { get; }

        public CommunicationCallbackParityVerdict Verdict { get; }

        public bool HasParity =>
            Verdict == CommunicationCallbackParityVerdict.Parity;

        public int TypedLiveCount { get; }

        public int ScsLiveCount { get; }

        public DateTimeOffset ObservedAt { get; }

        internal static bool IsSupportedKind(
            WireV1.CommunicationCallbackKind kind)
        {
            return kind != WireV1.CommunicationCallbackKind.Unspecified &&
                   Enum.IsDefined(
                       typeof(WireV1.CommunicationCallbackKind),
                       kind);
        }

        internal static bool IsCanonicalNonEmptyGuid(string value)
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

        internal static bool IsValidIdentity(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Length > 128 ||
                !string.Equals(
                    value,
                    value.Trim(),
                    StringComparison.Ordinal))
            {
                return false;
            }

            foreach (char character in value)
            {
                if (char.IsControl(character))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public static class CommunicationCallbackKindParityComparator
    {
        public static CommunicationCallbackKindParityEvidence Compare(
            WireV1.CommunicationCallbackKind kind,
            CommunicationCallbackParityWindow typedWindow,
            CommunicationCallbackParityWindow scsWindow,
            DateTimeOffset observedAt)
        {
            if (!CommunicationCallbackKindParityEvidence.IsSupportedKind(kind))
            {
                throw new InvalidOperationException(
                    "Callback kind parity requires a known callback kind.");
            }
            if (typedWindow == null)
            {
                throw new ArgumentNullException(nameof(typedWindow));
            }
            if (scsWindow == null)
            {
                throw new ArgumentNullException(nameof(scsWindow));
            }

            CommunicationCallbackParityWindow filteredTyped =
                FilterWindow(typedWindow, kind);
            CommunicationCallbackParityWindow filteredScs =
                FilterWindow(scsWindow, kind);
            CommunicationCallbackParityReport report =
                CommunicationCallbackParityComparator.Compare(
                    filteredTyped,
                    filteredScs);

            return new CommunicationCallbackKindParityEvidence(
                report.ProcessIdentity,
                kind,
                ResolveGeneration(
                    filteredTyped,
                    filteredScs,
                    report),
                report.Verdict,
                report.TypedLiveCount,
                report.ScsLiveCount,
                observedAt);
        }

        private static CommunicationCallbackParityWindow FilterWindow(
            CommunicationCallbackParityWindow window,
            WireV1.CommunicationCallbackKind kind)
        {
            var samples =
                new List<CommunicationCallbackParitySample>();
            foreach (CommunicationCallbackParitySample sample in
                     window.LiveSamples)
            {
                if (sample.Kind == kind)
                {
                    samples.Add(sample);
                }
            }

            return new CommunicationCallbackParityWindow(
                window.Source,
                window.ProcessIdentity,
                window.RuntimeGenerationId,
                window.IsActive,
                window.ReplayEvidence,
                samples.Count,
                window.EvictedObservations,
                samples);
        }

        private static string ResolveGeneration(
            CommunicationCallbackParityWindow typedWindow,
            CommunicationCallbackParityWindow scsWindow,
            CommunicationCallbackParityReport report)
        {
            if (!string.IsNullOrEmpty(report.RuntimeGenerationId))
            {
                return report.RuntimeGenerationId;
            }
            if (string.Equals(
                    typedWindow.RuntimeGenerationId,
                    scsWindow.RuntimeGenerationId,
                    StringComparison.Ordinal))
            {
                return typedWindow.RuntimeGenerationId;
            }

            // Evidence construction requires one canonical generation. When
            // the source windows disagree, retain the typed generation while
            // the mismatch verdict prevents qualification.
            return typedWindow.RuntimeGenerationId;
        }
    }

    public sealed class CommunicationCallbackCutoverGate
    {
        public const int DefaultRequiredParityWindows = 3;
        public const int MaximumRequiredParityWindows = 16;

        private readonly object _syncRoot = new object();
        private readonly WireV1.CommunicationCallbackKind _targetKind;
        private readonly int _requiredParityWindows;
        private readonly HashSet<string> _qualifiedGenerations =
            new HashSet<string>(StringComparer.Ordinal);
        private int _state =
            (int)CommunicationCallbackCutoverState.ScsAuthoritative;
        private string _qualifiedIdentity = string.Empty;
        private string _activeGeneration = string.Empty;

        public CommunicationCallbackCutoverGate(
            WireV1.CommunicationCallbackKind targetKind,
            int requiredParityWindows = DefaultRequiredParityWindows)
        {
            if (targetKind !=
                    WireV1.CommunicationCallbackKind.PenaltyRefresh ||
                requiredParityWindows <= 0 ||
                requiredParityWindows > MaximumRequiredParityWindows)
            {
                throw new InvalidOperationException(
                    "The first callback cutover gate supports only PenaltyRefresh and a bounded parity-window requirement.");
            }

            _targetKind = targetKind;
            _requiredParityWindows = requiredParityWindows;
        }

        public WireV1.CommunicationCallbackKind TargetKind =>
            _targetKind;

        public int RequiredParityWindows =>
            _requiredParityWindows;

        public CommunicationCallbackCutoverState State =>
            (CommunicationCallbackCutoverState)
                Volatile.Read(ref _state);

        public string QualifiedIdentity
        {
            get
            {
                lock (_syncRoot)
                {
                    return _qualifiedIdentity;
                }
            }
        }

        public string ActiveGeneration
        {
            get
            {
                lock (_syncRoot)
                {
                    return _activeGeneration;
                }
            }
        }

        public bool Arm(
            IReadOnlyList<CommunicationCallbackKindParityEvidence> evidence)
        {
            if (evidence == null)
            {
                throw new ArgumentNullException(nameof(evidence));
            }

            lock (_syncRoot)
            {
                if ((CommunicationCallbackCutoverState)_state !=
                    CommunicationCallbackCutoverState.ScsAuthoritative)
                {
                    return false;
                }
                if (evidence.Count < _requiredParityWindows)
                {
                    return false;
                }

                int firstIndex =
                    evidence.Count - _requiredParityWindows;
                string identity = null;
                DateTimeOffset previousObservedAt =
                    DateTimeOffset.MinValue;
                var generations =
                    new HashSet<string>(StringComparer.Ordinal);

                for (int index = firstIndex;
                     index < evidence.Count;
                     index++)
                {
                    CommunicationCallbackKindParityEvidence item =
                        evidence[index] ??
                        throw new InvalidOperationException(
                            "Callback cutover qualification contains null evidence.");

                    if (item.Kind != _targetKind ||
                        !item.HasParity ||
                        item.TypedLiveCount == 0 ||
                        item.TypedLiveCount != item.ScsLiveCount ||
                        item.ObservedAt <= previousObservedAt ||
                        !generations.Add(item.RuntimeGenerationId))
                    {
                        return false;
                    }

                    if (identity == null)
                    {
                        identity = item.ProcessIdentity;
                    }
                    else if (!string.Equals(
                                 identity,
                                 item.ProcessIdentity,
                                 StringComparison.Ordinal))
                    {
                        return false;
                    }

                    previousObservedAt = item.ObservedAt;
                }

                _qualifiedIdentity = identity ?? string.Empty;
                _qualifiedGenerations.Clear();
                foreach (string generation in generations)
                {
                    _qualifiedGenerations.Add(generation);
                }
                Volatile.Write(
                    ref _state,
                    (int)CommunicationCallbackCutoverState.Armed);
                return true;
            }
        }

        public bool Activate(
            string processIdentity,
            string runtimeGenerationId)
        {
            if (!CommunicationCallbackKindParityEvidence
                    .IsValidIdentity(processIdentity) ||
                !CommunicationCallbackKindParityEvidence
                    .IsCanonicalNonEmptyGuid(runtimeGenerationId))
            {
                throw new InvalidOperationException(
                    "Callback cutover activation identity or generation is invalid.");
            }

            lock (_syncRoot)
            {
                if ((CommunicationCallbackCutoverState)_state !=
                    CommunicationCallbackCutoverState.Armed)
                {
                    return false;
                }
                if (!string.Equals(
                        processIdentity,
                        _qualifiedIdentity,
                        StringComparison.Ordinal) ||
                    _qualifiedGenerations.Contains(runtimeGenerationId))
                {
                    return false;
                }

                _activeGeneration = runtimeGenerationId;
                Volatile.Write(
                    ref _state,
                    (int)CommunicationCallbackCutoverState
                        .TypedGrpcAuthoritative);
                return true;
            }
        }

        public bool Rollback()
        {
            lock (_syncRoot)
            {
                CommunicationCallbackCutoverState state =
                    (CommunicationCallbackCutoverState)_state;
                if (state ==
                        CommunicationCallbackCutoverState
                            .ScsAuthoritative ||
                    state ==
                        CommunicationCallbackCutoverState.RolledBack)
                {
                    return false;
                }

                _activeGeneration = string.Empty;
                Volatile.Write(
                    ref _state,
                    (int)CommunicationCallbackCutoverState.RolledBack);
                return true;
            }
        }

        public bool ShouldApply(
            CommunicationCallbackParitySource source,
            WireV1.CommunicationCallbackKind kind)
        {
            if (!Enum.IsDefined(
                    typeof(CommunicationCallbackParitySource),
                    source) ||
                !CommunicationCallbackKindParityEvidence
                    .IsSupportedKind(kind))
            {
                throw new InvalidOperationException(
                    "Callback cutover authority query is malformed.");
            }

            if (kind != _targetKind)
            {
                return source ==
                    CommunicationCallbackParitySource.LegacyScs;
            }

            bool typedAuthority =
                State ==
                CommunicationCallbackCutoverState
                    .TypedGrpcAuthoritative;
            return typedAuthority
                ? source ==
                  CommunicationCallbackParitySource.TypedGrpc
                : source ==
                  CommunicationCallbackParitySource.LegacyScs;
        }
    }
}
