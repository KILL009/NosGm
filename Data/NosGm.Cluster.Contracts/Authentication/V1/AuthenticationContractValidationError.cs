namespace NosGm.Cluster.Contracts.Authentication.V1
{
    public enum AuthenticationContractValidationError
    {
        None = 0,
        MissingRequest = 1,
        InvalidContext = 2,
        InvalidCallerRole = 3,
        InvalidAccountName = 4,
        InvalidAuthorizationCode = 5,
        InvalidInstallationId = 6,
        InvalidCountryId = 7,
        InvalidAccountId = 8,
        InvalidSessionId = 9,
        InvalidIpAddress = 10
    }
}
