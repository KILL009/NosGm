using System;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Cluster.Contracts.Communication.V1
{
    public static class CommunicationCallbackRuntimeInfoContractValidator
    {
        public static CommunicationCallbackContractValidationError Validate(
            WireV1.GetCommunicationCallbackRuntimeInfoRequest request)
        {
            if (request == null)
            {
                return CommunicationCallbackContractValidationError.MissingRequest;
            }
            if (request.Context?.Version == null ||
                request.Context.Version.Major > ushort.MaxValue ||
                request.Context.Version.Minor > ushort.MaxValue)
            {
                return CommunicationCallbackContractValidationError.InvalidContext;
            }

            var context = new ClusterRequestContext
            {
                Version = new ClusterContractVersion(
                    (ushort)request.Context.Version.Major,
                    (ushort)request.Context.Version.Minor),
                RequestId = request.Context.RequestId,
                IssuedAtUnixTimeMilliseconds =
                    request.Context.IssuedAtUnixTimeMs,
                DeadlineUnixTimeMilliseconds =
                    request.Context.DeadlineUnixTimeMs,
                CallerRole = (ClusterNodeRole)request.Context.CallerRole,
                RequestedService =
                    (ClusterService)request.Context.RequestedService,
                CallerInstanceId = request.Context.CallerInstanceId
            };
            if (ClusterContractValidator.Validate(context) !=
                    ClusterContractValidationError.None ||
                context.RequestedService != ClusterService.Communication)
            {
                return CommunicationCallbackContractValidationError.InvalidContext;
            }

            return context.CallerRole == ClusterNodeRole.Login ||
                   context.CallerRole == ClusterNodeRole.World
                ? CommunicationCallbackContractValidationError.None
                : CommunicationCallbackContractValidationError
                    .InvalidCallerRole;
        }
    }
}
