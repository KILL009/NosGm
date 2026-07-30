namespace NosGm.Cluster.Contracts.Communication.V1
{
    public enum CommunicationCallbackContractValidationError
    {
        None = 0,
        MissingRequest = 1,
        InvalidContext = 2,
        InvalidCallerRole = 3,
        InvalidSubscriberIdentity = 4,
        InvalidAcceptedKinds = 5,
        InvalidEventId = 6,
        InvalidEventTtl = 7,
        InvalidTarget = 8,
        MissingCallback = 9,
        InvalidCallbackPayload = 10,
        TargetCallbackMismatch = 11
    }
}
