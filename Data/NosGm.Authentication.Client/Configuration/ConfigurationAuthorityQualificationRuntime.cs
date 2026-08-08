using System;
using System.Collections.Generic;

namespace NosGm.Authentication.Client.Configuration
{
    public sealed class ConfigurationAuthorityQualificationStatus
    {
        internal ConfigurationAuthorityQualificationStatus(
            int capacity,
            int retainedRuntimeCount,
            long acceptedReports,
            long replacedReports,
            long evictedReports,
            bool invalidated,
            ConfigurationUpdateParityReport lastReport,
            Exception lastException)
        {
            Capacity = capacity;
            RetainedRuntimeCount = retainedRuntimeCount;
            AcceptedReports = acceptedReports;
            ReplacedReports = replacedReports;
            EvictedReports = evictedReports;
            IsInvalidated = invalidated;
            LastReport = lastReport;
            LastException = lastException;
        }

        public int Capacity { get; }

        public int RetainedRuntimeCount { get; }

        public long AcceptedReports { get; }

        public long ReplacedReports { get; }

        public long EvictedReports { get; }

        public bool IsInvalidated { get; }

        public ConfigurationUpdateParityReport LastReport { get; }

        public Exception LastException { get; }
    }

    public sealed class ConfigurationAuthorityQualificationRuntime
    {
        public const int DefaultEvidenceCapacity = 16;
        public const int MaximumEvidenceCapacity = 64;

        private static readonly Lazy<ConfigurationAuthorityQualificationRuntime>
            LazyInstance =
                new Lazy<ConfigurationAuthorityQualificationRuntime>(
                    () =>
                        new ConfigurationAuthorityQualificationRuntime(
                            ConfigurationAuthorityCoordinator.Instance));

        private readonly int _capacity;
        private readonly ConfigurationAuthorityCoordinator _coordinator;
        private readonly LinkedList<ConfigurationUpdateParityReport>
            _evidence =
                new LinkedList<ConfigurationUpdateParityReport>();
        private readonly object _syncRoot = new object();
        private long _acceptedReports;
        private bool _configured;
        private long _evictedReports;
        private bool _invalidated;
        private ConfigurationUpdateParityReport _lastReport;
        private Exception _lastException;
        private string _processGenerationId = string.Empty;
        private long _replacedReports;

        public ConfigurationAuthorityQualificationRuntime(
            ConfigurationAuthorityCoordinator coordinator,
            int evidenceCapacity = DefaultEvidenceCapacity)
        {
            if (evidenceCapacity <
                    ConfigurationAuthorityGate.DefaultRequiredParityWindows ||
                evidenceCapacity > MaximumEvidenceCapacity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(evidenceCapacity),
                    "Configuration qualification evidence capacity must be between " +
                    ConfigurationAuthorityGate.DefaultRequiredParityWindows +
                    " and " + MaximumEvidenceCapacity + ".");
            }

            _coordinator = coordinator ??
                throw new ArgumentNullException(nameof(coordinator));
            _capacity = evidenceCapacity;
        }

        public static ConfigurationAuthorityQualificationRuntime Instance =>
            LazyInstance.Value;

        public bool TryConfigureFromEnvironment(
            out string diagnostic)
        {
            try
            {
                ConfigurationUpdateObservationLedger ledger =
                    ConfigurationUpdateObservationLedger.Instance;
                ConfigurationAuthorityOperatorOptions options =
                    ConfigurationAuthorityOperatorOptions.Load();
                Configure(
                    ledger.ProcessGenerationId,
                    options);
                diagnostic = options.RollbackRequested
                    ? "rollback-requested"
                    : options.EffectRoutingRequested
                        ? "effect-routing-requested"
                        : options.HasArmRequest
                            ? "armed-for-qualification"
                            : "unarmed";
                return true;
            }
            catch (Exception exception)
            {
                Invalidate(exception);
                diagnostic = exception.GetType().Name;
                return false;
            }
        }

        public bool Configure(
            string processGenerationId,
            ConfigurationAuthorityOperatorOptions options)
        {
            bool configured = _coordinator.Configure(
                processGenerationId,
                options);
            lock (_syncRoot)
            {
                if (!_configured)
                {
                    _configured = true;
                    _processGenerationId = processGenerationId;
                }
                else if (!string.Equals(
                             _processGenerationId,
                             processGenerationId,
                             StringComparison.Ordinal))
                {
                    var exception = new InvalidOperationException(
                        "Configuration qualification process generation changed inside one runtime.");
                    InvalidateLocked(exception);
                    throw exception;
                }
            }
            return configured;
        }

        public bool ObserveParity(ConfigurationUpdateParityReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            IReadOnlyList<ConfigurationUpdateParityReport> snapshot = null;
            lock (_syncRoot)
            {
                _lastReport = report;
                if (!_configured || _invalidated)
                {
                    return false;
                }
                if (!string.Equals(
                        _processGenerationId,
                        report.ProcessGenerationId,
                        StringComparison.Ordinal))
                {
                    InvalidateLocked(
                        new InvalidOperationException(
                            "Configuration parity evidence belongs to another process generation."));
                    return false;
                }
                if (report.HasTerminalMismatch)
                {
                    InvalidateLocked(
                        new InvalidOperationException(
                            "Configuration parity evidence failed terminally with " +
                            report.Verdict + "."));
                    return false;
                }
                if (!report.HasParity)
                {
                    return false;
                }

                LinkedListNode<ConfigurationUpdateParityReport> existing =
                    FindRuntimeLocked(report.RuntimeGenerationId);
                if (existing != null)
                {
                    if (report.EvaluatedThroughLedgerOrdinal <=
                        existing.Value.EvaluatedThroughLedgerOrdinal)
                    {
                        return false;
                    }
                    existing.Value = report;
                    _replacedReports++;
                }
                else
                {
                    if (_evidence.Count == _capacity)
                    {
                        _evidence.RemoveFirst();
                        _evictedReports++;
                    }
                    _evidence.AddLast(report);
                    _acceptedReports++;
                }

                snapshot = new List<ConfigurationUpdateParityReport>(
                    _evidence);
                _lastException = null;
            }

            return _coordinator.ObserveQualification(snapshot);
        }

        public bool ObserveTypedUpdate(
            string processGenerationId,
            ConfigurationTransportUpdate update)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            bool activated = _coordinator.ObserveRuntimeGeneration(
                processGenerationId,
                update.RuntimeGenerationId);
            if (update.RecoveredFromSnapshot)
            {
                _coordinator.CompleteRecovery(update.RuntimeGenerationId);
            }
            return activated;
        }

        public bool ObserveStreamEnded(
            string runtimeGenerationId,
            Exception reason = null)
        {
            return _coordinator.ObserveStreamEnded(
                runtimeGenerationId,
                reason);
        }

        public void Invalidate(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            lock (_syncRoot)
            {
                InvalidateLocked(exception);
            }
        }

        public IReadOnlyList<ConfigurationUpdateParityReport>
            GetEvidenceSnapshot()
        {
            lock (_syncRoot)
            {
                return new List<ConfigurationUpdateParityReport>(_evidence)
                    .AsReadOnly();
            }
        }

        public ConfigurationAuthorityQualificationStatus GetStatus()
        {
            lock (_syncRoot)
            {
                return new ConfigurationAuthorityQualificationStatus(
                    _capacity,
                    _evidence.Count,
                    _acceptedReports,
                    _replacedReports,
                    _evictedReports,
                    _invalidated,
                    _lastReport,
                    _lastException);
            }
        }

        private LinkedListNode<ConfigurationUpdateParityReport>
            FindRuntimeLocked(string runtimeGenerationId)
        {
            LinkedListNode<ConfigurationUpdateParityReport> node =
                _evidence.First;
            while (node != null)
            {
                if (string.Equals(
                        node.Value.RuntimeGenerationId,
                        runtimeGenerationId,
                        StringComparison.Ordinal))
                {
                    return node;
                }
                node = node.Next;
            }
            return null;
        }

        private void InvalidateLocked(Exception exception)
        {
            _invalidated = true;
            _lastException = exception;
            _coordinator.RequestRollback(exception);
        }
    }
}
