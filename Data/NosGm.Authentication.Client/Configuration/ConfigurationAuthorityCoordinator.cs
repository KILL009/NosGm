using System;
using System.Collections.Generic;

namespace NosGm.Authentication.Client.Configuration
{
    public sealed class ConfigurationAuthorityStatus
    {
        internal ConfigurationAuthorityStatus(
            bool blocked,
            bool typedIngressReady,
            string lastObservedRuntimeGenerationId,
            ConfigurationAuthorityState state,
            string qualifiedProcessGenerationId,
            string activeRuntimeGenerationId,
            int pendingOverlapUpdates,
            long overlapUpdatesRecorded,
            long overlapDuplicatesSuppressed,
            long overlapUpdatesExpired,
            Exception lastException)
        {
            Blocked = blocked;
            TypedIngressReady = typedIngressReady;
            LastObservedRuntimeGenerationId =
                lastObservedRuntimeGenerationId ?? string.Empty;
            State = state;
            QualifiedProcessGenerationId =
                qualifiedProcessGenerationId ?? string.Empty;
            ActiveRuntimeGenerationId =
                activeRuntimeGenerationId ?? string.Empty;
            PendingOverlapUpdates = pendingOverlapUpdates;
            OverlapUpdatesRecorded = overlapUpdatesRecorded;
            OverlapDuplicatesSuppressed = overlapDuplicatesSuppressed;
            OverlapUpdatesExpired = overlapUpdatesExpired;
            LastException = lastException;
        }

        public bool Blocked { get; }

        public bool TypedIngressReady { get; }

        public string LastObservedRuntimeGenerationId { get; }

        public ConfigurationAuthorityState State { get; }

        public string QualifiedProcessGenerationId { get; }

        public string ActiveRuntimeGenerationId { get; }

        public int PendingOverlapUpdates { get; }

        public long OverlapUpdatesRecorded { get; }

        public long OverlapDuplicatesSuppressed { get; }

        public long OverlapUpdatesExpired { get; }

        public Exception LastException { get; }
    }

    public sealed class ConfigurationAuthorityCoordinator
    {
        private readonly ConfigurationAuthorityGate _gate;
        private readonly ConfigurationUpdateOverlapDeduplicationLedger
            _overlapLedger;
        private readonly object _syncRoot = new object();
        private bool _blocked;
        private string _lastObservedRuntimeGenerationId = string.Empty;
        private Exception _lastException;
        private bool _typedIngressReady;

        public ConfigurationAuthorityCoordinator()
            : this(
                new ConfigurationAuthorityGate(),
                new ConfigurationUpdateOverlapDeduplicationLedger())
        {
        }

        public ConfigurationAuthorityCoordinator(
            int requiredParityWindows,
            int overlapCapacity,
            TimeSpan? overlapRetention = null)
            : this(
                new ConfigurationAuthorityGate(requiredParityWindows),
                new ConfigurationUpdateOverlapDeduplicationLedger(
                    overlapCapacity,
                    overlapRetention))
        {
        }

        internal ConfigurationAuthorityCoordinator(
            ConfigurationAuthorityGate gate,
            ConfigurationUpdateOverlapDeduplicationLedger overlapLedger)
        {
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _overlapLedger = overlapLedger ??
                throw new ArgumentNullException(nameof(overlapLedger));
        }

        public bool ObserveQualification(
            IReadOnlyList<ConfigurationUpdateParityReport> evidence)
        {
            if (evidence == null)
            {
                throw new ArgumentNullException(nameof(evidence));
            }

            lock (_syncRoot)
            {
                if (_blocked ||
                    _gate.State !=
                        ConfigurationAuthorityState.ScsAuthoritative)
                {
                    return false;
                }

                try
                {
                    bool armed = _gate.Arm(evidence);
                    if (armed)
                    {
                        _lastException = null;
                    }
                    return armed;
                }
                catch (Exception exception)
                {
                    FailClosed(exception);
                    return false;
                }
            }
        }

        public bool ObserveRuntimeGeneration(
            string processGenerationId,
            string runtimeGenerationId)
        {
            if (!ConfigurationAuthorityGate.IsCanonicalNonEmptyGuid(
                    processGenerationId) ||
                !ConfigurationAuthorityGate.IsCanonicalNonEmptyGuid(
                    runtimeGenerationId))
            {
                throw new InvalidOperationException(
                    "The observed Configuration authority generation is malformed.");
            }

            lock (_syncRoot)
            {
                _lastObservedRuntimeGenerationId = runtimeGenerationId;
                if (_blocked)
                {
                    return false;
                }

                ConfigurationAuthorityState state = _gate.State;
                if (state == ConfigurationAuthorityState.ScsAuthoritative ||
                    state == ConfigurationAuthorityState.RolledBack)
                {
                    return false;
                }
                if (state == ConfigurationAuthorityState.Armed)
                {
                    _typedIngressReady = false;
                    try
                    {
                        bool activated = _gate.Activate(
                            processGenerationId,
                            runtimeGenerationId);
                        if (activated)
                        {
                            _lastException = null;
                        }
                        return activated;
                    }
                    catch (Exception exception)
                    {
                        FailClosed(exception);
                        return false;
                    }
                }
                if (string.Equals(
                        _gate.ActiveRuntimeGenerationId,
                        runtimeGenerationId,
                        StringComparison.Ordinal))
                {
                    return false;
                }

                FailClosed(
                    new InvalidOperationException(
                        "The Configuration runtime changed after typed authority activation."));
                return false;
            }
        }

        public bool CompleteRecovery(string runtimeGenerationId)
        {
            if (!ConfigurationAuthorityGate.IsCanonicalNonEmptyGuid(
                    runtimeGenerationId))
            {
                throw new InvalidOperationException(
                    "The completed Configuration recovery generation is malformed.");
            }

            lock (_syncRoot)
            {
                if (_blocked ||
                    _gate.State !=
                        ConfigurationAuthorityState.TypedGrpcAuthoritative)
                {
                    return false;
                }
                if (!string.Equals(
                        _gate.ActiveRuntimeGenerationId,
                        runtimeGenerationId,
                        StringComparison.Ordinal))
                {
                    FailClosed(
                        new InvalidOperationException(
                            "Configuration recovery does not belong to the active runtime."));
                    return false;
                }

                _typedIngressReady = true;
                _lastException = null;
                return true;
            }
        }

        public bool ObserveStreamEnded(
            string runtimeGenerationId,
            Exception reason = null)
        {
            if (!string.IsNullOrEmpty(runtimeGenerationId) &&
                !ConfigurationAuthorityGate.IsCanonicalNonEmptyGuid(
                    runtimeGenerationId))
            {
                throw new InvalidOperationException(
                    "The ended Configuration stream generation is malformed.");
            }

            lock (_syncRoot)
            {
                bool wasReady = _typedIngressReady;
                _typedIngressReady = false;
                if (_gate.State ==
                    ConfigurationAuthorityState.TypedGrpcAuthoritative)
                {
                    FailClosed(
                        reason ??
                        new InvalidOperationException(
                            "The active typed Configuration stream ended."));
                }
                return wasReady;
            }
        }

        public bool RequestRollback(Exception reason = null)
        {
            lock (_syncRoot)
            {
                _blocked = true;
                _typedIngressReady = false;
                _lastException = reason ??
                    new InvalidOperationException(
                        "Configuration authority rollback was requested.");
                return _gate.Rollback();
            }
        }

        public bool ShouldUse(
            ConfigurationAuthoritySource source,
            ConfigurationAuthorityOperation operation)
        {
            ConfigurationAuthorityGate.ValidateSourceAndOperation(
                source,
                operation);
            lock (_syncRoot)
            {
                return ShouldUseCore(source, operation);
            }
        }

        public bool TryApplyCallback(
            ConfigurationAuthoritySource source,
            ConfigurationTransportSnapshot snapshot,
            Action applyEffect)
        {
            if (applyEffect == null)
            {
                throw new ArgumentNullException(nameof(applyEffect));
            }
            ConfigurationAuthorityGate.ValidateSourceAndOperation(
                source,
                ConfigurationAuthorityOperation.Callback);
            string semanticFingerprint =
                ConfigurationSnapshotSemanticFingerprint.Compute(snapshot);

            lock (_syncRoot)
            {
                DateTimeOffset observedAt = DateTimeOffset.UtcNow;
                ConfigurationAuthorityState state = _gate.State;
                bool overlapEnabled =
                    state == ConfigurationAuthorityState.Armed ||
                    state ==
                        ConfigurationAuthorityState.TypedGrpcAuthoritative;

                if (state == ConfigurationAuthorityState.RolledBack)
                {
                    if (source == ConfigurationAuthoritySource.Scs &&
                        _overlapLedger.TryConsumeOpposite(
                            source,
                            semanticFingerprint,
                            observedAt))
                    {
                        return false;
                    }
                    if (source != ConfigurationAuthoritySource.Scs)
                    {
                        return false;
                    }

                    applyEffect();
                    return true;
                }

                if (!overlapEnabled)
                {
                    if (source != ConfigurationAuthoritySource.Scs)
                    {
                        return false;
                    }

                    applyEffect();
                    return true;
                }

                if (_overlapLedger.TryConsumeOpposite(
                        source,
                        semanticFingerprint,
                        observedAt))
                {
                    return false;
                }

                bool typedSelected =
                    source == ConfigurationAuthoritySource.TypedGrpc &&
                    !_blocked &&
                    _typedIngressReady &&
                    state ==
                        ConfigurationAuthorityState.TypedGrpcAuthoritative;
                bool scsSelected =
                    source == ConfigurationAuthoritySource.Scs;
                if (!typedSelected && !scsSelected)
                {
                    return false;
                }

                if (!_overlapLedger.HasCapacity(observedAt))
                {
                    FailClosed(
                        new InvalidOperationException(
                            "Configuration overlap evidence reached its bounded capacity."));
                    if (source == ConfigurationAuthoritySource.TypedGrpc)
                    {
                        return false;
                    }

                    applyEffect();
                    return true;
                }

                try
                {
                    applyEffect();
                    _overlapLedger.RecordApplied(
                        source,
                        semanticFingerprint,
                        observedAt);
                    return true;
                }
                catch (Exception exception)
                {
                    if (source == ConfigurationAuthoritySource.TypedGrpc)
                    {
                        FailClosed(exception);
                    }
                    throw;
                }
            }
        }

        public ConfigurationAuthorityStatus GetStatus()
        {
            lock (_syncRoot)
            {
                return new ConfigurationAuthorityStatus(
                    _blocked,
                    _typedIngressReady,
                    _lastObservedRuntimeGenerationId,
                    _gate.State,
                    _gate.QualifiedProcessGenerationId,
                    _gate.ActiveRuntimeGenerationId,
                    _overlapLedger.PendingCount,
                    _overlapLedger.Recorded,
                    _overlapLedger.DuplicatesSuppressed,
                    _overlapLedger.Expired,
                    _lastException);
            }
        }

        private bool ShouldUseCore(
            ConfigurationAuthoritySource source,
            ConfigurationAuthorityOperation operation)
        {
            if (_blocked ||
                !_typedIngressReady ||
                _gate.State !=
                    ConfigurationAuthorityState.TypedGrpcAuthoritative)
            {
                return source == ConfigurationAuthoritySource.Scs;
            }

            return _gate.ShouldUse(source, operation);
        }

        private void FailClosed(Exception exception)
        {
            _blocked = true;
            _typedIngressReady = false;
            _lastException = exception ??
                new InvalidOperationException(
                    "Configuration authority failed closed.");
            _gate.Rollback();
        }
    }
}
