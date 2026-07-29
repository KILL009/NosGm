namespace NosGm.Cluster.Contracts.Communication.V1
{
    public enum CommunicationContractValidationError
    {
        None = 0,
        MissingRequest = 1,
        InvalidContext = 2,
        InvalidCallerRole = 3,
        InvalidAccountId = 4,
        InvalidSessionId = 5,
        InvalidCharacterId = 6,
        InvalidWorldId = 7,
        InvalidIpAddress = 8,
        InvalidEndpointPort = 9,
        InvalidAccountLimit = 10,
        InvalidWorldGroup = 11,
        InvalidPreserveSessionRequest = 12,
        MissingWorldRegistration = 13
    }
}
