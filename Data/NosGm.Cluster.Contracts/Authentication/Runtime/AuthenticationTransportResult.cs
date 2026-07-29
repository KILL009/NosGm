namespace NosGm.Cluster.Contracts.Authentication.Runtime
{
    public enum AuthenticationTransportResultCode
    {
        Unspecified = 0,
        Success = 1,
        InvalidRequest = 2,
        Unauthorized = 3,
        Disabled = 4,
        NotFoundOrExpired = 5,
        Conflict = 6,
        CapacityExceeded = 7
    }

    public sealed class AuthenticationTicketConsumptionResult
    {
        public AuthenticationTransportResultCode Result { get; set; }

        public string AccountName { get; set; }

        public int ConsumptionNumber { get; set; }

        public int SessionId { get; set; }

        public bool IsSuccess =>
            Result == AuthenticationTransportResultCode.Success;
    }
}
