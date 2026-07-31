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
    }
}
