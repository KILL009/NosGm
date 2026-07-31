using System;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationCallbackOperatorCutoverStatus
    {
        internal CommunicationCallbackOperatorCutoverStatus(
            WireV1.CommunicationCallbackKind targetKind,
            bool isConfigured,
            bool isBlocked,
            bool operatorRollbackRequested,
            bool effectRoutingEnabled,
            bool typedIngressReady,
            int pendingOverlapEffects,
            long overlapEffectsRecorded,
            long overlapDuplicatesSuppressed,
            long overlapEffectsExpired,
            string configuredIdentity,
            string armRequestId,
            string lastObservedGeneration,
            CommunicationCallbackCutoverState state,
            string qualifiedIdentity,
            string activeGeneration,
            Exception lastException)
        {
            TargetKind = targetKind;
            IsConfigured = isConfigured;
            IsBlocked = isBlocked;
            OperatorRollbackRequested = operatorRollbackRequested;
            EffectRoutingEnabled = effectRoutingEnabled;
            TypedIngressReady = typedIngressReady;
            PendingOverlapEffects = pendingOverlapEffects;
            OverlapEffectsRecorded = overlapEffectsRecorded;
            OverlapDuplicatesSuppressed = overlapDuplicatesSuppressed;
            OverlapEffectsExpired = overlapEffectsExpired;
            ConfiguredIdentity = configuredIdentity ?? string.Empty;
            ArmRequestId = armRequestId ?? string.Empty;
            LastObservedGeneration =
                lastObservedGeneration ?? string.Empty;
            State = state;
            QualifiedIdentity = qualifiedIdentity ?? string.Empty;
            ActiveGeneration = activeGeneration ?? string.Empty;
            LastException = lastException;
        }

        public WireV1.CommunicationCallbackKind TargetKind { get; }

        public bool IsConfigured { get; }

        public bool IsBlocked { get; }

        public bool OperatorRollbackRequested { get; }

        public bool EffectRoutingEnabled { get; }

        public bool TypedIngressReady { get; }

        public int PendingOverlapEffects { get; }

        public long OverlapEffectsRecorded { get; }

        public long OverlapDuplicatesSuppressed { get; }

        public long OverlapEffectsExpired { get; }

        public string ConfiguredIdentity { get; }

        public string ArmRequestId { get; }

        public bool HasArmRequest =>
            !string.IsNullOrEmpty(ArmRequestId);

        public string LastObservedGeneration { get; }

        public CommunicationCallbackCutoverState State { get; }

        public string QualifiedIdentity { get; }

        public string ActiveGeneration { get; }

        public Exception LastException { get; }
    }

    public sealed class CommunicationCallbackOperatorCutoverCoordinator
    {
        private static readonly Lazy<
                CommunicationCallbackOperatorCutoverCoordinator>
            LazyInstance =
                new Lazy<CommunicationCallbackOperatorCutoverCoordinator>(
                    () =>
                        new CommunicationCallbackOperatorCutoverCoordinator());

        private readonly object _syncRoot = new object();
        private readonly CommunicationCallbackCutoverGate _gate;
        private readonly CommunicationCallbackOverlapDeduplicationLedger
            _overlapLedger;
        private bool _configured;
        private bool _blocked;
        private bool _operatorRollbackRequested;
        private bool _effectRoutingEnabled;
        private bool _typedIngressReady;
        private string _configuredIdentity = string.Empty;
        private string _armRequestId = string.Empty;
        private string _lastObservedGeneration = string.Empty;
        private Exception _lastException;

        public CommunicationCallbackOperatorCutoverCoordinator()
            : this(
                new CommunicationCallbackCutoverGate(
                    WireV1.CommunicationCallbackKind.PenaltyRefresh),
                new CommunicationCallbackOverlapDeduplicationLedger())
        {
        }

        internal CommunicationCallbackOperatorCutoverCoordinator(
            CommunicationCallbackCutoverGate gate)
            : this(
                gate,
                new CommunicationCallbackOverlapDeduplicationLedger())
        {
        }

        internal CommunicationCallbackOperatorCutoverCoordinator(
            CommunicationCallbackCutoverGate gate,
            CommunicationCallbackOverlapDeduplicationLedger overlapLedger)
        {
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _overlapLedger = overlapLedger ??
                throw new ArgumentNullException(nameof(overlapLedger));
        }

        public static CommunicationCallbackOperatorCutoverCoordinator
            Instance => LazyInstance.Value;

        public bool Configure(
            string processIdentity,
            CommunicationCallbackOperatorCutoverOptions options)
        {
            return Configure(
                processIdentity,
                options,
                effectRoutingEnabled: false);
        }

        public bool Configure(
            string processIdentity,
            CommunicationCallbackOperatorCutoverOptions options,
            bool effectRoutingEnabled)
        {
            ValidateConfiguration(processIdentity, options);
            lock (_syncRoot)
            {
                if (_configured)
                {
                    bool sameConfiguration =
                        IsSameOperatorConfiguration(
                            processIdentity,
                            options) &&
                        _effectRoutingEnabled == effectRoutingEnabled;
                    if (sameConfiguration)
                    {
                        return false;
                    }

                    var exception = new InvalidOperationException(
                        "Operator callback cutover configuration changed inside one process.");
                    FailClosed(exception);
                    throw exception;
                }

                _configured = true;
                _configuredIdentity = processIdentity;
                _armRequestId =
                    options.PenaltyRefreshArmRequestId;
                _operatorRollbackRequested =
                    options.PenaltyRefreshRollbackRequested;
                _effectRoutingEnabled = effectRoutingEnabled;
                if (_operatorRollbackRequested)
                {
                    _blocked = true;
                    _lastException = new InvalidOperationException(
                        "The operator requested PenaltyRefresh callback rollback before activation.");
                }
                return true;
            }
        }

        public bool ObserveQualification(
            CommunicationCallbackKindParityEvidenceLedger evidenceLedger)
        {
            if (evidenceLedger == null)
            {
                throw new ArgumentNullException(nameof(evidenceLedger));
            }

            lock (_syncRoot)
            {
                if (!_configured ||
                    _blocked ||
                    string.IsNullOrEmpty(_armRequestId) ||
                    _gate.State !=
                        CommunicationCallbackCutoverState.ScsAuthoritative)
                {
                    return false;
                }

                try
                {
                    bool armed = evidenceLedger.TryArm(_gate);
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
            string runtimeGenerationId)
        {
            if (!CommunicationCallbackKindParityEvidence
                    .IsCanonicalNonEmptyGuid(runtimeGenerationId))
            {
                throw new InvalidOperationException(
                    "The observed callback runtime generation is invalid.");
            }

            lock (_syncRoot)
            {
                _typedIngressReady = false;
                _lastObservedGeneration = runtimeGenerationId;
                if (!_configured || _blocked)
                {
                    return false;
                }

                CommunicationCallbackCutoverState state = _gate.State;
                if (state ==
                    CommunicationCallbackCutoverState.ScsAuthoritative)
                {
                    return false;
                }
                if (state == CommunicationCallbackCutoverState.Armed)
                {
                    try
                    {
                        bool activated = _gate.Activate(
                            _configuredIdentity,
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
                if (state ==
                    CommunicationCallbackCutoverState.TypedGrpcAuthoritative)
                {
                    if (string.Equals(
                            _gate.ActiveGeneration,
                            runtimeGenerationId,
                            StringComparison.Ordinal))
                    {
                        return false;
                    }

                    FailClosed(
                        new InvalidOperationException(
                            "The callback runtime generation changed after PenaltyRefresh authority activation."));
                    return false;
                }

                return false;
            }
        }

        public bool CompleteReplay(string runtimeGenerationId)
        {
            if (!CommunicationCallbackKindParityEvidence
                    .IsCanonicalNonEmptyGuid(runtimeGenerationId))
            {
                throw new InvalidOperationException(
                    "The completed callback replay generation is invalid.");
            }

            lock (_syncRoot)
            {
                if (!_configured ||
                    _blocked ||
                    !_effectRoutingEnabled ||
                    _gate.State !=
                        CommunicationCallbackCutoverState
                            .TypedGrpcAuthoritative)
                {
                    return false;
                }
                if (!string.Equals(
                        _gate.ActiveGeneration,
                        runtimeGenerationId,
                        StringComparison.Ordinal))
                {
                    FailClosed(
                        new InvalidOperationException(
                            "Replay completion does not belong to the active PenaltyRefresh generation."));
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
                !CommunicationCallbackKindParityEvidence
                    .IsCanonicalNonEmptyGuid(runtimeGenerationId))
            {
                throw new InvalidOperationException(
                    "The ended callback stream generation is invalid.");
            }

            lock (_syncRoot)
            {
                bool wasReady = _typedIngressReady;
                _typedIngressReady = false;
                if (_effectRoutingEnabled &&
                    _gate.State ==
                        CommunicationCallbackCutoverState
                            .TypedGrpcAuthoritative)
                {
                    FailClosed(
                        reason ??
                        new InvalidOperationException(
                            "The active typed PenaltyRefresh callback stream ended."));
                }
                return wasReady;
            }
        }

        public bool ObserveSubscriberFault(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            lock (_syncRoot)
            {
                if (!_effectRoutingEnabled ||
                    _gate.State !=
                        CommunicationCallbackCutoverState
                            .TypedGrpcAuthoritative)
                {
                    return false;
                }

                FailClosed(exception);
                return true;
            }
        }

        public bool RequestRollback(Exception reason = null)
        {
            lock (_syncRoot)
            {
                _operatorRollbackRequested = true;
                _blocked = true;
                _typedIngressReady = false;
                _lastException = reason ??
                    new InvalidOperationException(
                        "The operator requested PenaltyRefresh callback rollback.");
                return _gate.Rollback();
            }
        }

        public bool ShouldApply(
            CommunicationCallbackParitySource source,
            WireV1.CommunicationCallbackKind kind)
        {
            lock (_syncRoot)
            {
                return ShouldApplyCore(source, kind);
            }
        }

        public bool TryApply(
            CommunicationCallbackParitySource source,
            WireV1.CommunicationCallbackKind kind,
            Action applyEffect)
        {
            if (applyEffect == null)
            {
                throw new ArgumentNullException(nameof(applyEffect));
            }

            lock (_syncRoot)
            {
                if (!ShouldApplyCore(source, kind))
                {
                    return false;
                }

                try
                {
                    applyEffect();
                    return true;
                }
                catch (Exception exception)
                {
                    if (source ==
                            CommunicationCallbackParitySource.TypedGrpc &&
                        kind == _gate.TargetKind)
                    {
                        FailClosed(exception);
                    }
                    throw;
                }
            }
        }

        public bool TryApply(
            CommunicationCallbackParitySource source,
            WireV1.CommunicationCallbackKind kind,
            string semanticFingerprint,
            Action applyEffect)
        {
            if (applyEffect == null)
            {
                throw new ArgumentNullException(nameof(applyEffect));
            }

            lock (_syncRoot)
            {
                if (kind != _gate.TargetKind || !_effectRoutingEnabled)
                {
                    if (!ShouldApplyCore(source, kind))
                    {
                        return false;
                    }
                    applyEffect();
                    return true;
                }

                DateTimeOffset observedAt = DateTimeOffset.UtcNow;
                if (_overlapLedger.TryConsumeOpposite(
                        source,
                        semanticFingerprint,
                        observedAt))
                {
                    return false;
                }

                bool typedSelected =
                    source == CommunicationCallbackParitySource.TypedGrpc &&
                    !_blocked &&
                    _typedIngressReady &&
                    _gate.State ==
                        CommunicationCallbackCutoverState
                            .TypedGrpcAuthoritative;
                bool legacySelected =
                    source == CommunicationCallbackParitySource.LegacyScs;
                if (!typedSelected && !legacySelected)
                {
                    return false;
                }

                if (!_overlapLedger.HasCapacity(observedAt))
                {
                    FailClosed(
                        new InvalidOperationException(
                            "PenaltyRefresh overlap evidence reached its bounded capacity."));
                    if (source == CommunicationCallbackParitySource.TypedGrpc)
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
                    if (source ==
                        CommunicationCallbackParitySource.TypedGrpc)
                    {
                        FailClosed(exception);
                    }
                    throw;
                }
            }
        }

        public CommunicationCallbackOperatorCutoverStatus GetStatus()
        {
            lock (_syncRoot)
            {
                return new CommunicationCallbackOperatorCutoverStatus(
                    _gate.TargetKind,
                    _configured,
                    _blocked,
                    _operatorRollbackRequested,
                    _effectRoutingEnabled,
                    _typedIngressReady,
                    _overlapLedger.PendingCount,
                    _overlapLedger.Recorded,
                    _overlapLedger.DuplicatesSuppressed,
                    _overlapLedger.Expired,
                    _configuredIdentity,
                    _armRequestId,
                    _lastObservedGeneration,
                    _gate.State,
                    _gate.QualifiedIdentity,
                    _gate.ActiveGeneration,
                    _lastException);
            }
        }

        private bool ShouldApplyCore(
            CommunicationCallbackParitySource source,
            WireV1.CommunicationCallbackKind kind)
        {
            if (kind != _gate.TargetKind ||
                !_effectRoutingEnabled ||
                !_typedIngressReady ||
                _blocked)
            {
                return source ==
                    CommunicationCallbackParitySource.LegacyScs;
            }

            return _gate.ShouldApply(source, kind);
        }

        private bool IsSameOperatorConfiguration(
            string processIdentity,
            CommunicationCallbackOperatorCutoverOptions options)
        {
            return string.Equals(
                       _configuredIdentity,
                       processIdentity,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       _armRequestId,
                       options.PenaltyRefreshArmRequestId,
                       StringComparison.Ordinal) &&
                   _operatorRollbackRequested ==
                       options.PenaltyRefreshRollbackRequested;
        }

        private static void ValidateConfiguration(
            string processIdentity,
            CommunicationCallbackOperatorCutoverOptions options)
        {
            if (!CommunicationCallbackKindParityEvidence
                    .IsValidIdentity(processIdentity))
            {
                throw new InvalidOperationException(
                    "The operator cutover process identity is invalid.");
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
        }

        private void FailClosed(Exception exception)
        {
            _blocked = true;
            _typedIngressReady = false;
            _lastException = exception;
            _gate.Rollback();
        }
    }
}
