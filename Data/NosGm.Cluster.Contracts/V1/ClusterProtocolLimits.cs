namespace NosGm.Cluster.Contracts.V1
{
    public static class ClusterProtocolLimits
    {
        public const int MaxInboundMessageBytes = 4 * 1024 * 1024;
        public const int MaxOutboundMessageBytes = 4 * 1024 * 1024;
        public const int MaxMetadataEntryBytes = 8 * 1024;
        public const int MaxCallerInstanceIdLength = 128;
        public const int RequestIdLength = 36;
        public const int DefaultDeadlineMilliseconds = 10 * 1000;
        public const int MaxDeadlineMilliseconds = 60 * 1000;
        public const int MaxClockSkewMilliseconds = 30 * 1000;
        public const int MaxConcurrentCallsPerConnection = 256;
        public const int BoundedDispatchQueueCapacity = 2048;
    }
}
