using System;
using System.Collections.Generic;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationCallbackQualificationStatus
    {
        internal CommunicationCallbackQualificationStatus(
            WireV1.CommunicationCallbackKind targetKind,
            int capacity,
            long appendedEvidence,
            long evictedEvidence,
            bool invalidated,
            bool hasCompleteHistory,
            bool isQualified,
            CommunicationCallbackKindParityEvidence lastEvidence,
            Exception lastException)
        {
            TargetKind = targetKind;
            Capacity = capacity;
            AppendedEvidence = appendedEvidence;
            EvictedEvidence = evictedEvidence;
            IsInvalidated = invalidated;
            HasCompleteHistory = hasCompleteHistory;
            IsQualified = isQualified;
            LastEvidence = lastEvidence;
            LastException = lastException;
        }

        public WireV1.CommunicationCallbackKind TargetKind { get; }

        public int Capacity { get; }

        public long AppendedEvidence { get; }

        public long EvictedEvidence { get; }

        public bool IsInvalidated { get; }

        public bool HasCompleteHistory { get; }

        public bool IsQualified { get; }

        public CommunicationCallbackKindParityEvidence LastEvidence { get; }

        public Exception LastException { get; }
    }

    public sealed class CommunicationCallbackQualificationRuntime
    {
        private static readonly Lazy<CommunicationCallbackQualificationRuntime>
            LazyInstance =
                new Lazy<CommunicationCallbackQualificationRuntime>(
                    () => new CommunicationCallbackQualificationRuntime());

        private readonly object _syncRoot = new object();
        private readonly CommunicationCallbackKindParityEvidenceLedger
            _penaltyRefreshEvidence =
                new CommunicationCallbackKindParityEvidenceLedger(
                    WireV1.CommunicationCallbackKind.PenaltyRefresh);
        private CommunicationCallbackKindParityEvidence _lastEvidence;
        private Exception _lastException;

        private CommunicationCallbackQualificationRuntime()
        {
        }

        public static CommunicationCallbackQualificationRuntime Instance =>
            LazyInstance.Value;

        public bool TryCapturePenaltyRefresh(
            CommunicationCallbackParityWindow typedWindow,
            CommunicationCallbackParityWindow scsWindow,
            DateTimeOffset observedAt,
            out CommunicationCallbackKindParityEvidence evidence)
        {
            if (typedWindow == null)
            {
                throw new ArgumentNullException(nameof(typedWindow));
            }
            if (scsWindow == null)
            {
                throw new ArgumentNullException(nameof(scsWindow));
            }

            try
            {
                evidence =
                    CommunicationCallbackKindParityComparator.Compare(
                        WireV1.CommunicationCallbackKind.PenaltyRefresh,
                        typedWindow,
                        scsWindow,
                        observedAt);
                CommunicationCallbackOperatorCutoverCoordinator coordinator =
                    CommunicationCallbackOperatorCutoverCoordinator.Instance;
                coordinator.EnsureConfigured(
                    evidence.ProcessIdentity,
                    CommunicationCallbackOperatorCutoverOptions.Load());
                bool appended = _penaltyRefreshEvidence.TryAppend(evidence);
                coordinator.ObserveQualification(_penaltyRefreshEvidence);
                lock (_syncRoot)
                {
                    _lastEvidence = evidence;
                    _lastException = null;
                }
                return appended;
            }
            catch (Exception exception)
            {
                Invalidate(exception);
                evidence = null;
                return false;
            }
        }

        public void Invalidate(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            _penaltyRefreshEvidence.Invalidate();
            CommunicationCallbackOperatorCutoverCoordinator.Instance
                .RequestRollback(exception);
            lock (_syncRoot)
            {
                _lastException = exception;
            }
        }

        public IReadOnlyList<CommunicationCallbackKindParityEvidence>
            GetPenaltyRefreshEvidenceSnapshot()
        {
            return _penaltyRefreshEvidence.GetSnapshot();
        }

        public CommunicationCallbackQualificationStatus GetStatus()
        {
            CommunicationCallbackKindParityEvidence lastEvidence;
            Exception lastException;
            lock (_syncRoot)
            {
                lastEvidence = _lastEvidence;
                lastException = _lastException;
            }

            var gate = new CommunicationCallbackCutoverGate(
                WireV1.CommunicationCallbackKind.PenaltyRefresh);
            bool qualified = _penaltyRefreshEvidence.TryArm(gate);
            return new CommunicationCallbackQualificationStatus(
                _penaltyRefreshEvidence.TargetKind,
                _penaltyRefreshEvidence.Capacity,
                _penaltyRefreshEvidence.AppendedEvidence,
                _penaltyRefreshEvidence.EvictedEvidence,
                _penaltyRefreshEvidence.IsInvalidated,
                _penaltyRefreshEvidence.HasCompleteHistory,
                qualified,
                lastEvidence,
                lastException);
        }
    }
}
