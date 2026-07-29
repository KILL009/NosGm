using System;

namespace NosGm.Cluster.Contracts.V1
{
    public static class ClusterContractValidator
    {
        public static ClusterContractValidationError Validate(
            ClusterRequestContext context)
        {
            if (context == null)
            {
                return ClusterContractValidationError.MissingContext;
            }

            if (!context.Version.IsSupported)
            {
                return ClusterContractValidationError.UnsupportedVersion;
            }

            if (string.IsNullOrWhiteSpace(context.RequestId) ||
                context.RequestId.Length != ClusterProtocolLimits.RequestIdLength ||
                !Guid.TryParseExact(context.RequestId, "D", out _))
            {
                return ClusterContractValidationError.InvalidRequestId;
            }

            if (context.CallerRole == ClusterNodeRole.Unspecified ||
                !Enum.IsDefined(typeof(ClusterNodeRole), context.CallerRole))
            {
                return ClusterContractValidationError.InvalidCallerRole;
            }

            if (context.RequestedService == ClusterService.Unspecified ||
                !Enum.IsDefined(typeof(ClusterService), context.RequestedService))
            {
                return ClusterContractValidationError.InvalidService;
            }

            if (string.IsNullOrWhiteSpace(context.CallerInstanceId) ||
                context.CallerInstanceId.Length >
                ClusterProtocolLimits.MaxCallerInstanceIdLength)
            {
                return ClusterContractValidationError.InvalidCallerInstanceId;
            }

            if (context.IssuedAtUnixTimeMilliseconds <= 0)
            {
                return ClusterContractValidationError.InvalidIssuedAt;
            }

            long deadlineLength =
                context.DeadlineUnixTimeMilliseconds -
                context.IssuedAtUnixTimeMilliseconds;
            if (deadlineLength <= 0 ||
                deadlineLength >
                ClusterProtocolLimits.MaxDeadlineMilliseconds)
            {
                return ClusterContractValidationError.InvalidDeadline;
            }

            return ClusterContractValidationError.None;
        }

        public static ClusterContractValidationError ValidatePayloadLength(
            int payloadLength,
            bool outbound = false)
        {
            if (payloadLength < 0)
            {
                return ClusterContractValidationError.NegativePayloadLength;
            }

            int maximum = outbound
                ? ClusterProtocolLimits.MaxOutboundMessageBytes
                : ClusterProtocolLimits.MaxInboundMessageBytes;
            return payloadLength <= maximum
                ? ClusterContractValidationError.None
                : ClusterContractValidationError.PayloadTooLarge;
        }
    }
}
