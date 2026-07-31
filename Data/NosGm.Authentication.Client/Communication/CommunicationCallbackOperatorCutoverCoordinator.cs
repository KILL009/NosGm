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
        private bool _configured;
        private bool _blocked;
        private bool _operatorRollbackRequested;
        private string _configuredIdentity = string.Empty;
        private string _armRequestId = string.Empty;
        private string _lastObservedGeneration = string.Empty;
        private Exception _lastException;

        public CommunicationCallbackOperatorCutoverCoordinator()
            : this(
                new CommunicationCallbackCutoverGate(
                    WireV1.CommunicationCallbackKind.PenaltyRefresh))
        {
        }

        internal CommunicationCallbackOperatorCutoverCoordinator(
            CommunicationCallbackCutoverGate gate)
        {
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        }

        public static CommunicationCallbackOperatorCutoverCoordinator
            Instance => LazyInstance.Value;

        public bool Configure(
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

            lock (_syncRoot)
            {
                if (_configured)
                {
                    bool sameConfiguration =
                        string.Equals(
                            _configuredIdentity,
                            processIdentity,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            _armRequestId,
                            options.PenaltyRefreshArmRequestId,
                            StringComparison.Ordinal) &&
                        _operatorRollbackRequested ==
                            options.PenaltyRefreshRollbackRequested;
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

        public bool RequestRollback(Exception reason = null)
        {
            lock (_syncRoot)
            {
                _operatorRollbackRequested = true;
                _blocked = true;
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
            return _gate.ShouldApply(source, kind);
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
                    _configuredIdentity,
                    _armRequestId,
                    _lastObservedGeneration,
                    _gate.State,
                    _gate.QualifiedIdentity,
                    _gate.ActiveGeneration,
                    _lastException);
            }
        }

        private void FailClosed(Exception exception)
        {
            _blocked = true;
            _lastException = exception;
            _gate.Rollback();
        }
    }
}
