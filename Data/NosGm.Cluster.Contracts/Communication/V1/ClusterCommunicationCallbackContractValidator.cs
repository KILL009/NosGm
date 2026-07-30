using System;
using System.Collections.Generic;
using System.Linq;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Cluster.Contracts.Communication.V1
{
    public static class ClusterCommunicationCallbackContractValidator
    {
        public static CommunicationCallbackContractValidationError
            ValidateSubscribe(
                WireV1.SubscribeCommunicationCallbacksRequest request)
        {
            if (request == null)
            {
                return CommunicationCallbackContractValidationError.MissingRequest;
            }

            CommunicationCallbackContractValidationError contextError =
                ValidateContext(
                    request.Context,
                    ClusterNodeRole.Login,
                    ClusterNodeRole.World);
            if (contextError !=
                CommunicationCallbackContractValidationError.None)
            {
                return contextError;
            }

            ClusterNodeRole role =
                (ClusterNodeRole)request.Context.CallerRole;
            if (role == ClusterNodeRole.World)
            {
                if (!IsCanonicalWorldId(request.WorldId) ||
                    request.ChannelId <= 0 ||
                    !IsBoundedText(
                        request.WorldGroup,
                        CommunicationCallbackContractLimits.MaxWorldGroupLength))
                {
                    return CommunicationCallbackContractValidationError
                        .InvalidSubscriberIdentity;
                }
            }
            else if (!string.IsNullOrEmpty(request.WorldId) ||
                     request.ChannelId != 0 ||
                     !string.IsNullOrEmpty(request.WorldGroup))
            {
                return CommunicationCallbackContractValidationError
                    .InvalidSubscriberIdentity;
            }

            if (request.AcceptedKinds.Count >
                CommunicationCallbackContractLimits.MaxAcceptedKinds)
            {
                return CommunicationCallbackContractValidationError
                    .InvalidAcceptedKinds;
            }

            var distinctKinds = new HashSet<WireV1.CommunicationCallbackKind>();
            foreach (WireV1.CommunicationCallbackKind kind in
                     request.AcceptedKinds)
            {
                if (kind == WireV1.CommunicationCallbackKind.Unspecified ||
                    !Enum.IsDefined(
                        typeof(WireV1.CommunicationCallbackKind),
                        kind) ||
                    !distinctKinds.Add(kind) ||
                    !IsKindAllowedForSubscriber(role, kind))
                {
                    return CommunicationCallbackContractValidationError
                        .InvalidAcceptedKinds;
                }
            }

            return CommunicationCallbackContractValidationError.None;
        }

        public static CommunicationCallbackContractValidationError
            ValidatePublish(WireV1.PublishCommunicationCallbackRequest request)
        {
            if (request == null)
            {
                return CommunicationCallbackContractValidationError.MissingRequest;
            }

            CommunicationCallbackContractValidationError contextError =
                ValidateContext(request.Context, ClusterNodeRole.Master);
            if (contextError !=
                CommunicationCallbackContractValidationError.None)
            {
                return contextError;
            }

            if (!IsCanonicalNonEmptyGuid(request.EventId))
            {
                return CommunicationCallbackContractValidationError
                    .InvalidEventId;
            }

            if (request.TtlSeconds == 0 ||
                request.TtlSeconds >
                CommunicationCallbackContractLimits.MaxEventTtlSeconds)
            {
                return CommunicationCallbackContractValidationError
                    .InvalidEventTtl;
            }

            if (!ValidateTarget(request.Target))
            {
                return CommunicationCallbackContractValidationError
                    .InvalidTarget;
            }

            if (request.CallbackCase ==
                WireV1.PublishCommunicationCallbackRequest.CallbackOneofCase.None)
            {
                return CommunicationCallbackContractValidationError
                    .MissingCallback;
            }

            return ValidateCallbackAndTarget(request);
        }

        private static CommunicationCallbackContractValidationError
            ValidateCallbackAndTarget(
                WireV1.PublishCommunicationCallbackRequest request)
        {
            WireV1.CommunicationCallbackTargetKind targetKind =
                request.Target.Kind;
            switch (request.CallbackCase)
            {
                case WireV1.PublishCommunicationCallbackRequest
                    .CallbackOneofCase.CharacterPresence:
                    if (request.CharacterPresence.CharacterId <= 0)
                    {
                        return InvalidPayload();
                    }
                    return targetKind ==
                           WireV1.CommunicationCallbackTargetKind.WorldGroup
                        ? CommunicationCallbackContractValidationError.None
                        : TargetMismatch();

                case WireV1.PublishCommunicationCallbackRequest
                    .CallbackOneofCase.KickSession:
                    if ((!request.KickSession.HasAccountId &&
                         !request.KickSession.HasSessionId) ||
                        (request.KickSession.HasAccountId &&
                         request.KickSession.AccountId <= 0) ||
                        (request.KickSession.HasSessionId &&
                         request.KickSession.SessionId <= 0))
                    {
                        return InvalidPayload();
                    }
                    return targetKind ==
                           WireV1.CommunicationCallbackTargetKind.AllWorlds
                        ? CommunicationCallbackContractValidationError.None
                        : TargetMismatch();

                case WireV1.PublishCommunicationCallbackRequest
                    .CallbackOneofCase.Lifecycle:
                    if (!Enum.IsDefined(
                            typeof(WireV1.CommunicationLifecycleAction),
                            request.Lifecycle.Action) ||
                        request.Lifecycle.Action ==
                        WireV1.CommunicationLifecycleAction.Unspecified ||
                        request.Lifecycle.DelaySeconds >
                        CommunicationCallbackContractLimits
                            .MaxRestartDelaySeconds ||
                        (request.Lifecycle.Action ==
                         WireV1.CommunicationLifecycleAction.Shutdown &&
                         request.Lifecycle.DelaySeconds != 0))
                    {
                        return InvalidPayload();
                    }
                    return targetKind ==
                               WireV1.CommunicationCallbackTargetKind.AllWorlds ||
                           targetKind ==
                               WireV1.CommunicationCallbackTargetKind.WorldGroup
                        ? CommunicationCallbackContractValidationError.None
                        : TargetMismatch();

                case WireV1.PublishCommunicationCallbackRequest
                    .CallbackOneofCase.GlobalEvent:
                    if (!Enum.IsDefined(
                            typeof(WireV1.CommunicationGlobalEventType),
                            request.GlobalEvent.EventType) ||
                        request.GlobalEvent.EventType ==
                        WireV1.CommunicationGlobalEventType.Unspecified ||
                        request.GlobalEvent.Value >
                        CommunicationCallbackContractLimits.MaxGlobalEventValue)
                    {
                        return InvalidPayload();
                    }
                    return targetKind ==
                           WireV1.CommunicationCallbackTargetKind.AllWorlds
                        ? CommunicationCallbackContractValidationError.None
                        : TargetMismatch();

                case WireV1.PublishCommunicationCallbackRequest
                    .CallbackOneofCase.BazaarRefresh:
                    if (request.BazaarRefresh.BazaarItemId <= 0)
                    {
                        return InvalidPayload();
                    }
                    return targetKind ==
                           WireV1.CommunicationCallbackTargetKind.WorldGroup
                        ? CommunicationCallbackContractValidationError.None
                        : TargetMismatch();

                case WireV1.PublishCommunicationCallbackRequest
                    .CallbackOneofCase.FamilyRefresh:
                    if (request.FamilyRefresh.FamilyId <= 0)
                    {
                        return InvalidPayload();
                    }
                    return targetKind ==
                           WireV1.CommunicationCallbackTargetKind.WorldGroup
                        ? CommunicationCallbackContractValidationError.None
                        : TargetMismatch();

                case WireV1.PublishCommunicationCallbackRequest
                    .CallbackOneofCase.PenaltyRefresh:
                    if (request.PenaltyRefresh.PenaltyLogId <= 0)
                    {
                        return InvalidPayload();
                    }
                    return targetKind ==
                           WireV1.CommunicationCallbackTargetKind.AllNodes
                        ? CommunicationCallbackContractValidationError.None
                        : TargetMismatch();

                case WireV1.PublishCommunicationCallbackRequest
                    .CallbackOneofCase.RelationRefresh:
                    if (request.RelationRefresh.RelationId <= 0)
                    {
                        return InvalidPayload();
                    }
                    return targetKind ==
                           WireV1.CommunicationCallbackTargetKind.WorldGroup
                        ? CommunicationCallbackContractValidationError.None
                        : TargetMismatch();

                case WireV1.PublishCommunicationCallbackRequest
                    .CallbackOneofCase.StaticBonusRefresh:
                    if (request.StaticBonusRefresh.CharacterId <= 0)
                    {
                        return InvalidPayload();
                    }
                    return targetKind ==
                               WireV1.CommunicationCallbackTargetKind.CharacterId &&
                           request.Target.CharacterId ==
                               request.StaticBonusRefresh.CharacterId
                        ? CommunicationCallbackContractValidationError.None
                        : TargetMismatch();

                default:
                    return CommunicationCallbackContractValidationError
                        .MissingCallback;
            }
        }

        private static bool ValidateTarget(
            WireV1.CommunicationCallbackTarget target)
        {
            if (target == null ||
                !Enum.IsDefined(
                    typeof(WireV1.CommunicationCallbackTargetKind),
                    target.Kind) ||
                target.Kind ==
                    WireV1.CommunicationCallbackTargetKind.Unspecified)
            {
                return false;
            }

            switch (target.Kind)
            {
                case WireV1.CommunicationCallbackTargetKind.AllWorlds:
                case WireV1.CommunicationCallbackTargetKind.AllLoginNodes:
                case WireV1.CommunicationCallbackTargetKind.AllNodes:
                    return HasNoTargetDetails(target);

                case WireV1.CommunicationCallbackTargetKind.WorldGroup:
                    return IsBoundedText(
                               target.WorldGroup,
                               CommunicationCallbackContractLimits
                                   .MaxWorldGroupLength) &&
                           string.IsNullOrEmpty(target.WorldId) &&
                           target.CharacterId == 0;

                case WireV1.CommunicationCallbackTargetKind.WorldId:
                    return string.IsNullOrEmpty(target.WorldGroup) &&
                           IsCanonicalWorldId(target.WorldId) &&
                           target.CharacterId == 0;

                case WireV1.CommunicationCallbackTargetKind.CharacterId:
                    return string.IsNullOrEmpty(target.WorldGroup) &&
                           string.IsNullOrEmpty(target.WorldId) &&
                           target.CharacterId > 0;

                default:
                    return false;
            }
        }

        private static bool HasNoTargetDetails(
            WireV1.CommunicationCallbackTarget target)
        {
            return string.IsNullOrEmpty(target.WorldGroup) &&
                   string.IsNullOrEmpty(target.WorldId) &&
                   target.CharacterId == 0;
        }

        private static bool IsKindAllowedForSubscriber(
            ClusterNodeRole role,
            WireV1.CommunicationCallbackKind kind)
        {
            if (role == ClusterNodeRole.World)
            {
                return true;
            }

            return role == ClusterNodeRole.Login &&
                   kind == WireV1.CommunicationCallbackKind.PenaltyRefresh;
        }

        private static CommunicationCallbackContractValidationError
            ValidateContext(
                WireV1.RequestContext context,
                params ClusterNodeRole[] allowedRoles)
        {
            if (context?.Version == null ||
                context.Version.Major > ushort.MaxValue ||
                context.Version.Minor > ushort.MaxValue)
            {
                return CommunicationCallbackContractValidationError
                    .InvalidContext;
            }

            var contractContext = new ClusterRequestContext
            {
                Version = new ClusterContractVersion(
                    (ushort)context.Version.Major,
                    (ushort)context.Version.Minor),
                RequestId = context.RequestId,
                IssuedAtUnixTimeMilliseconds = context.IssuedAtUnixTimeMs,
                DeadlineUnixTimeMilliseconds = context.DeadlineUnixTimeMs,
                CallerRole = (ClusterNodeRole)context.CallerRole,
                RequestedService = (ClusterService)context.RequestedService,
                CallerInstanceId = context.CallerInstanceId
            };

            if (ClusterContractValidator.Validate(contractContext) !=
                    ClusterContractValidationError.None ||
                contractContext.RequestedService != ClusterService.Communication)
            {
                return CommunicationCallbackContractValidationError
                    .InvalidContext;
            }

            return allowedRoles.Contains(contractContext.CallerRole)
                ? CommunicationCallbackContractValidationError.None
                : CommunicationCallbackContractValidationError
                    .InvalidCallerRole;
        }

        private static bool IsCanonicalWorldId(string value)
        {
            return IsCanonicalNonEmptyGuid(value);
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

            return value.All(character => !char.IsControl(character));
        }

        private static CommunicationCallbackContractValidationError
            InvalidPayload()
        {
            return CommunicationCallbackContractValidationError
                .InvalidCallbackPayload;
        }

        private static CommunicationCallbackContractValidationError
            TargetMismatch()
        {
            return CommunicationCallbackContractValidationError
                .TargetCallbackMismatch;
        }
    }
}
