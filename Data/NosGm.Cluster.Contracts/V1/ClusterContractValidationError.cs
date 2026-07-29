namespace NosGm.Cluster.Contracts.V1
{
    public enum ClusterContractValidationError
    {
        None = 0,
        MissingContext = 1,
        UnsupportedVersion = 2,
        InvalidRequestId = 3,
        InvalidCallerRole = 4,
        InvalidService = 5,
        InvalidCallerInstanceId = 6,
        InvalidIssuedAt = 7,
        InvalidDeadline = 8,
        NegativePayloadLength = 9,
        PayloadTooLarge = 10
    }
}
