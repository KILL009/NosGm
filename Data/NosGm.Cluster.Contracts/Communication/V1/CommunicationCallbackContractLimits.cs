namespace NosGm.Cluster.Contracts.Communication.V1
{
    public static class CommunicationCallbackContractLimits
    {
        public const int MaxWorldGroupLength = 64;
        public const int MaxAcceptedKinds = 16;
        public const int MaxRetainedEventsPerSubscriber = 4096;
        public const int MaxPendingEventsPerSubscriber = 1024;
        public const uint DefaultEventTtlSeconds = 30;
        public const uint MaxEventTtlSeconds = 300;
        public const uint MaxRestartDelaySeconds = 3600;
        public const uint MaxGlobalEventValue = byte.MaxValue;
    }
}
