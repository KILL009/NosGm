using System;
using System.Collections.Generic;
using System.Threading;

namespace NosGm.Authentication.Client.Configuration
{
    public enum ConfigurationAuthoritySource
    {
        Scs = 1,
        TypedGrpc = 2
    }

    public enum ConfigurationAuthorityOperation
    {
        Get = 1,
        Update = 2,
        Callback = 3
    }

    public enum ConfigurationAuthorityState
    {
        ScsAuthoritative = 1,
        Armed = 2,
        TypedGrpcAuthoritative = 3,
        RolledBack = 4
    }

    public sealed class ConfigurationAuthorityGate
    {
        public const int DefaultRequiredParityWindows = 3;
        public const int MaximumRequiredParityWindows = 16;

        private readonly HashSet<string> _qualifiedRuntimeGenerations =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly int _requiredParityWindows;
        private readonly object _syncRoot = new object();
        private string _activeRuntimeGenerationId = string.Empty;
        private string _qualifiedProcessGenerationId = string.Empty;
        private int _state =
            (int)ConfigurationAuthorityState.ScsAuthoritative;

        public ConfigurationAuthorityGate(
            int requiredParityWindows = DefaultRequiredParityWindows)
        {
            if (requiredParityWindows <= 0 ||
                requiredParityWindows > MaximumRequiredParityWindows)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredParityWindows),
                    "Configuration authority requires between 1 and " +
                    MaximumRequiredParityWindows + " parity windows.");
            }

            _requiredParityWindows = requiredParityWindows;
        }

        public int RequiredParityWindows => _requiredParityWindows;

        public ConfigurationAuthorityState State =>
            (ConfigurationAuthorityState)Volatile.Read(ref _state);

        public string QualifiedProcessGenerationId
        {
            get
            {
                lock (_syncRoot)
                {
                    return _qualifiedProcessGenerationId;
                }
            }
        }

        public string ActiveRuntimeGenerationId
        {
            get
            {
                lock (_syncRoot)
                {
                    return _activeRuntimeGenerationId;
                }
            }
        }

        public bool Arm(
            IReadOnlyList<ConfigurationUpdateParityReport> evidence)
        {
            if (evidence == null)
            {
                throw new ArgumentNullException(nameof(evidence));
            }

            lock (_syncRoot)
            {
                if ((ConfigurationAuthorityState)_state !=
                    ConfigurationAuthorityState.ScsAuthoritative)
                {
                    return false;
                }
                if (evidence.Count < _requiredParityWindows)
                {
                    return false;
                }

                int firstIndex = evidence.Count - _requiredParityWindows;
                string processGenerationId = null;
                DateTimeOffset previousEvaluatedAt = DateTimeOffset.MinValue;
                var runtimeGenerations =
                    new HashSet<string>(StringComparer.Ordinal);
                for (int index = firstIndex;
                     index < evidence.Count;
                     index++)
                {
                    ConfigurationUpdateParityReport report = evidence[index];
                    if (!IsQualifyingReport(report) ||
                        report.EvaluatedAt <= previousEvaluatedAt ||
                        !runtimeGenerations.Add(
                            report.RuntimeGenerationId))
                    {
                        return false;
                    }

                    if (processGenerationId == null)
                    {
                        processGenerationId = report.ProcessGenerationId;
                    }
                    else if (!string.Equals(
                                 processGenerationId,
                                 report.ProcessGenerationId,
                                 StringComparison.Ordinal))
                    {
                        return false;
                    }

                    previousEvaluatedAt = report.EvaluatedAt;
                }

                _qualifiedProcessGenerationId =
                    processGenerationId ?? string.Empty;
                _qualifiedRuntimeGenerations.Clear();
                foreach (string runtimeGeneration in runtimeGenerations)
                {
                    _qualifiedRuntimeGenerations.Add(runtimeGeneration);
                }
                Volatile.Write(
                    ref _state,
                    (int)ConfigurationAuthorityState.Armed);
                return true;
            }
        }

        public bool Activate(
            string processGenerationId,
            string runtimeGenerationId)
        {
            if (!IsCanonicalNonEmptyGuid(processGenerationId) ||
                !IsCanonicalNonEmptyGuid(runtimeGenerationId))
            {
                throw new InvalidOperationException(
                    "Configuration authority activation identities are malformed.");
            }

            lock (_syncRoot)
            {
                if ((ConfigurationAuthorityState)_state !=
                    ConfigurationAuthorityState.Armed)
                {
                    return false;
                }
                if (!string.Equals(
                        _qualifiedProcessGenerationId,
                        processGenerationId,
                        StringComparison.Ordinal) ||
                    _qualifiedRuntimeGenerations.Contains(runtimeGenerationId))
                {
                    return false;
                }

                _activeRuntimeGenerationId = runtimeGenerationId;
                Volatile.Write(
                    ref _state,
                    (int)ConfigurationAuthorityState.TypedGrpcAuthoritative);
                return true;
            }
        }

        public bool Rollback()
        {
            lock (_syncRoot)
            {
                ConfigurationAuthorityState state =
                    (ConfigurationAuthorityState)_state;
                if (state == ConfigurationAuthorityState.ScsAuthoritative ||
                    state == ConfigurationAuthorityState.RolledBack)
                {
                    return false;
                }

                _activeRuntimeGenerationId = string.Empty;
                Volatile.Write(
                    ref _state,
                    (int)ConfigurationAuthorityState.RolledBack);
                return true;
            }
        }

        public bool ShouldUse(
            ConfigurationAuthoritySource source,
            ConfigurationAuthorityOperation operation)
        {
            ValidateSourceAndOperation(source, operation);
            bool typedAuthority =
                State == ConfigurationAuthorityState.TypedGrpcAuthoritative;
            return typedAuthority
                ? source == ConfigurationAuthoritySource.TypedGrpc
                : source == ConfigurationAuthoritySource.Scs;
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

        private static bool IsQualifyingReport(
            ConfigurationUpdateParityReport report)
        {
            return report != null &&
                   report.HasParity &&
                   !report.HasTerminalMismatch &&
                   IsCanonicalNonEmptyGuid(report.ProcessGenerationId) &&
                   IsCanonicalNonEmptyGuid(report.RuntimeGenerationId) &&
                   report.EvaluatedAt != default(DateTimeOffset) &&
                   report.EvictedObservations == 0 &&
                   report.WindowStartLedgerOrdinal > 0 &&
                   report.EvaluatedThroughLedgerOrdinal >=
                       report.WindowStartLedgerOrdinal &&
                   report.ScsLiveCount > 0 &&
                   report.ScsLiveCount == report.GrpcLiveCount &&
                   report.MatchedLiveCount == report.ScsLiveCount &&
                   !report.FirstMismatchIndex.HasValue &&
                   !report.OldestUnmatchedAgeMilliseconds.HasValue;
        }

        internal static void ValidateSourceAndOperation(
            ConfigurationAuthoritySource source,
            ConfigurationAuthorityOperation operation)
        {
            if (!Enum.IsDefined(
                    typeof(ConfigurationAuthoritySource),
                    source) ||
                !Enum.IsDefined(
                    typeof(ConfigurationAuthorityOperation),
                    operation))
            {
                throw new InvalidOperationException(
                    "Configuration authority query is malformed.");
            }
        }
    }
}
