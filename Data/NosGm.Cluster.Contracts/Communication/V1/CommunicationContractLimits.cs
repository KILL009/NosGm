namespace NosGm.Cluster.Contracts.Communication.V1
{
    public static class CommunicationContractLimits
    {
        public const int MaxIpAddressLength = 45;
        public const int MaxWorldGroupLength = 64;
        public const uint MaxEndpointPort = 65535;
        public const uint MaxAccountLimit = 100000;
        public const int MaxWorldsPerResponse = 1024;
        public const uint MaxCharacterCount = 4;
    }
}
