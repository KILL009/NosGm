namespace NosGm.Cluster.Contracts.V1
{
    public sealed class ClusterRequestContext
    {
        public ClusterContractVersion Version { get; set; } =
            ClusterContractVersion.Current;

        public string RequestId { get; set; }

        public long IssuedAtUnixTimeMilliseconds { get; set; }

        public long DeadlineUnixTimeMilliseconds { get; set; }

        public ClusterNodeRole CallerRole { get; set; }

        public ClusterService RequestedService { get; set; }

        public string CallerInstanceId { get; set; }
    }
}
