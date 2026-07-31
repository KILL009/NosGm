using NosGm.Communication.Client;
using System;
using System.Collections.Generic;

namespace NosGm.Master.Library.Client
{
    public static class
        CommunicationCallbackSubscriberLifecycleQualificationExtensions
    {
        public static CommunicationCallbackQualificationStatus
            GetPenaltyRefreshQualificationStatus(
                this CommunicationCallbackSubscriberLifecycle lifecycle)
        {
            if (lifecycle == null)
            {
                throw new ArgumentNullException(nameof(lifecycle));
            }

            return CommunicationCallbackQualificationRuntime.Instance
                .GetStatus();
        }

        public static IReadOnlyList<
                CommunicationCallbackKindParityEvidence>
            GetPenaltyRefreshQualificationEvidenceSnapshot(
                this CommunicationCallbackSubscriberLifecycle lifecycle)
        {
            if (lifecycle == null)
            {
                throw new ArgumentNullException(nameof(lifecycle));
            }

            return CommunicationCallbackQualificationRuntime.Instance
                .GetPenaltyRefreshEvidenceSnapshot();
        }

        public static CommunicationCallbackOperatorCutoverStatus
            GetPenaltyRefreshOperatorCutoverStatus(
                this CommunicationCallbackSubscriberLifecycle lifecycle)
        {
            if (lifecycle == null)
            {
                throw new ArgumentNullException(nameof(lifecycle));
            }

            return CommunicationCallbackOperatorCutoverCoordinator.Instance
                .GetStatus();
        }

        public static bool RequestPenaltyRefreshOperatorRollback(
            this CommunicationCallbackSubscriberLifecycle lifecycle,
            string reason)
        {
            if (lifecycle == null)
            {
                throw new ArgumentNullException(nameof(lifecycle));
            }
            if (string.IsNullOrWhiteSpace(reason) ||
                !string.Equals(
                    reason,
                    reason.Trim(),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "A bounded operator rollback reason is required.");
            }
            if (reason.Length > 256)
            {
                throw new InvalidOperationException(
                    "The operator rollback reason cannot exceed 256 characters.");
            }

            return CommunicationCallbackOperatorCutoverCoordinator.Instance
                .RequestRollback(
                    new InvalidOperationException(
                        "Operator rollback: " + reason));
        }
    }
}
