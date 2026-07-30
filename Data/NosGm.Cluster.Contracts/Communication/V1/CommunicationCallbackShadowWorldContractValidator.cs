using System;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Cluster.Contracts.Communication.V1
{
    public static class CommunicationCallbackShadowWorldContractValidator
    {
        public static CommunicationCallbackContractValidationError ValidateRegister(
            WireV1.RegisterCommunicationCallbackShadowWorldRequest request)
        {
            if (request == null)
            {
                return CommunicationCallbackContractValidationError.MissingRequest;
            }
            CommunicationCallbackContractValidationError contextError =
                ValidateWorldContext(request.Context);
            if (contextError != CommunicationCallbackContractValidationError.None)
            {
                return contextError;
            }
            if (!IsCanonicalNonEmptyGuid(request.RuntimeGenerationId) ||
                !IsCanonicalNonEmptyGuid(request.WorldId) ||
                request.ChannelId <= 0 ||
                !IsBoundedText(
                    request.WorldGroup,
                    CommunicationCallbackContractLimits.MaxWorldGroupLength))
            {
                return CommunicationCallbackContractValidationError
                    .InvalidSubscriberIdentity;
            }
            return CommunicationCallbackContractValidationError.None;
        }

        public static CommunicationCallbackContractValidationError ValidateUnregister(
            WireV1.UnregisterCommunicationCallbackShadowWorldRequest request)
        {
            if (request == null)
            {
                return CommunicationCallbackContractValidationError.MissingRequest;
            }
            CommunicationCallbackContractValidationError contextError =
                ValidateWorldContext(request.Context);
            if (contextError != CommunicationCallbackContractValidationError.None)
            {
                return contextError;
            }
            return IsCanonicalNonEmptyGuid(request.RuntimeGenerationId) &&
                   IsCanonicalNonEmptyGuid(request.WorldId)
                ? CommunicationCallbackContractValidationError.None
                : CommunicationCallbackContractValidationError
                    .InvalidSubscriberIdentity;
        }

        private static CommunicationCallbackContractValidationError
            ValidateWorldContext(WireV1.RequestContext wireContext)
        {
            if (wireContext?.Version == null ||
                wireContext.Version.Major > ushort.MaxValue ||
                wireContext.Version.Minor > ushort.MaxValue)
            {
                return CommunicationCallbackContractValidationError.InvalidContext;
            }

            var context = new ClusterRequestContext
            {
                Version = new ClusterContractVersion(
                    (ushort)wireContext.Version.Major,
                    (ushort)wireContext.Version.Minor),
                RequestId = wireContext.RequestId,
                IssuedAtUnixTimeMilliseconds = wireContext.IssuedAtUnixTimeMs,
                DeadlineUnixTimeMilliseconds = wireContext.DeadlineUnixTimeMs,
                CallerRole = (ClusterNodeRole)wireContext.CallerRole,
                RequestedService =
                    (ClusterService)wireContext.RequestedService,
                CallerInstanceId = wireContext.CallerInstanceId
            };
            if (ClusterContractValidator.Validate(context) !=
                    ClusterContractValidationError.None ||
                context.RequestedService != ClusterService.Communication)
            {
                return CommunicationCallbackContractValidationError.InvalidContext;
            }
            return context.CallerRole == ClusterNodeRole.World
                ? CommunicationCallbackContractValidationError.None
                : CommunicationCallbackContractValidationError.InvalidCallerRole;
        }

        private static bool IsCanonicalNonEmptyGuid(string value)
        {
            return value != null &&
                   value.Length == 36 &&
                   Guid.TryParseExact(value, "D", out Guid parsed) &&
                   parsed != Guid.Empty &&
                   string.Equals(
                       parsed.ToString("D"),
                       value,
                       StringComparison.Ordinal);
        }

        private static bool IsBoundedText(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Length > maximumLength ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                return false;
            }
            foreach (char character in value)
            {
                if (char.IsControl(character))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
