namespace NosGm.Cluster.Contracts.Authentication.V1
{
    public static class AuthenticationContractLimits
    {
        public const int MaxAccountNameLength = 255;
        public const int MaxAuthorizationCodeLength = 4096;
        public const uint MaxCountryId = 9;
        public const int InstallationIdLength = 36;
        public const int MaxIpAddressLength = 45;
    }
}
