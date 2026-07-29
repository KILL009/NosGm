using System;
using System.Globalization;
using System.Net;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Cluster.Contracts.Communication.V1
{
    public static class ClusterCommunicationContractValidator
    {
        public static CommunicationContractValidationError Validate(
            WireV1.RegisterAccountLoginRequest request)
        {
            CommunicationContractValidationError error = ValidateRequest(
                request,
                request?.Context,
                ClusterNodeRole.Login);
            if (error != CommunicationContractValidationError.None)
            {
                return error;
            }

            error = ValidateAccountSession(request.AccountId, request.SessionId);
            return error != CommunicationContractValidationError.None
                ? error
                : ValidateIpAddress(request.IpAddress);
        }

        public static CommunicationContractValidationError
            ValidateAccountSessionRegistered(WireV1.AccountSessionRequest request)
        {
            return ValidateAccountSessionRequest(request, ClusterNodeRole.Login);
        }

        public static CommunicationContractValidationError
            ValidateLoginPermitted(WireV1.AccountSessionRequest request)
        {
            return ValidateAccountSessionRequest(request, ClusterNodeRole.World);
        }

        public static CommunicationContractValidationError
            ValidateAccountConnected(WireV1.AccountRequest request)
        {
            CommunicationContractValidationError error = ValidateRequest(
                request,
                request?.Context,
                ClusterNodeRole.Login,
                ClusterNodeRole.World);
            return error != CommunicationContractValidationError.None
                ? error
                : ValidateAccountId(request.AccountId);
        }

        public static CommunicationContractValidationError Validate(
            WireV1.ConnectAccountRequest request)
        {
            CommunicationContractValidationError error = ValidateRequest(
                request,
                request?.Context,
                ClusterNodeRole.World);
            if (error != CommunicationContractValidationError.None)
            {
                return error;
            }

            error = ValidateWorldId(request.WorldId);
            return error != CommunicationContractValidationError.None
                ? error
                : ValidateAccountSession(request.AccountId, request.SessionId);
        }

        public static CommunicationContractValidationError Validate(
            WireV1.DisconnectAccountRequest request)
        {
            CommunicationContractValidationError error = ValidateRequest(
                request,
                request?.Context,
                ClusterNodeRole.Login,
                ClusterNodeRole.World);
            if (error != CommunicationContractValidationError.None)
            {
                return error;
            }

            error = ValidateAccountId(request.AccountId);
            if (error != CommunicationContractValidationError.None)
            {
                return error;
            }

            if (request.SessionId < 0)
            {
                return CommunicationContractValidationError.InvalidSessionId;
            }

            return request.PreserveSessionRegistration && request.SessionId <= 0
                ? CommunicationContractValidationError
                    .InvalidPreserveSessionRequest
                : CommunicationContractValidationError.None;
        }

        public static CommunicationContractValidationError ValidatePulse(
            WireV1.AccountRequest request)
        {
            CommunicationContractValidationError error = ValidateRequest(
                request,
                request?.Context,
                ClusterNodeRole.World);
            return error != CommunicationContractValidationError.None
                ? error
                : ValidateAccountId(request.AccountId);
        }

        public static CommunicationContractValidationError ValidatePulse(
            WireV1.AccountSessionRequest request)
        {
            return ValidateAccountSessionRequest(request, ClusterNodeRole.World);
        }

        public static CommunicationContractValidationError ValidateConnectCharacter(
            WireV1.CharacterWorldRequest request)
        {
            return ValidateCharacterWorldRequest(request);
        }

        public static CommunicationContractValidationError ValidateDisconnectCharacter(
            WireV1.CharacterWorldRequest request)
        {
            return ValidateCharacterWorldRequest(request);
        }

        public static CommunicationContractValidationError Validate(
            WireV1.RegisterWorldServerRequest request)
        {
            CommunicationContractValidationError error = ValidateRequest(
                request,
                request?.Context,
                ClusterNodeRole.World);
            if (error != CommunicationContractValidationError.None)
            {
                return error;
            }

            if (request.World == null)
            {
                return CommunicationContractValidationError
                    .MissingWorldRegistration;
            }

            error = ValidateWorldId(request.World.WorldId);
            if (error != CommunicationContractValidationError.None)
            {
                return error;
            }

            error = ValidateIpAddress(request.World.EndpointIp);
            if (error != CommunicationContractValidationError.None)
            {
                return error;
            }

            if (request.World.EndpointPort == 0 ||
                request.World.EndpointPort >
                CommunicationContractLimits.MaxEndpointPort)
            {
                return CommunicationContractValidationError.InvalidEndpointPort;
            }

            if (request.World.AccountLimit == 0 ||
                request.World.AccountLimit >
                CommunicationContractLimits.MaxAccountLimit)
            {
                return CommunicationContractValidationError.InvalidAccountLimit;
            }

            return IsBoundedText(
                    request.World.WorldGroup,
                    CommunicationContractLimits.MaxWorldGroupLength)
                ? CommunicationContractValidationError.None
                : CommunicationContractValidationError.InvalidWorldGroup;
        }

        public static CommunicationContractValidationError Validate(
            WireV1.WorldRequest request)
        {
            CommunicationContractValidationError error = ValidateRequest(
                request,
                request?.Context,
                ClusterNodeRole.World);
            return error != CommunicationContractValidationError.None
                ? error
                : ValidateWorldId(request.WorldId);
        }

        public static CommunicationContractValidationError Validate(
            WireV1.ListWorldServersRequest request)
        {
            return ValidateRequest(
                request,
                request?.Context,
                ClusterNodeRole.Login);
        }

        private static CommunicationContractValidationError
            ValidateAccountSessionRequest(
                WireV1.AccountSessionRequest request,
                ClusterNodeRole role)
        {
            CommunicationContractValidationError error = ValidateRequest(
                request,
                request?.Context,
                role);
            return error != CommunicationContractValidationError.None
                ? error
                : ValidateAccountSession(request.AccountId, request.SessionId);
        }

        private static CommunicationContractValidationError
            ValidateCharacterWorldRequest(WireV1.CharacterWorldRequest request)
        {
            CommunicationContractValidationError error = ValidateRequest(
                request,
                request?.Context,
                ClusterNodeRole.World);
            if (error != CommunicationContractValidationError.None)
            {
                return error;
            }

            error = ValidateWorldId(request.WorldId);
            if (error != CommunicationContractValidationError.None)
            {
                return error;
            }

            // Account/session fields are mandatory for the new runtime adapters,
            // but remain zero-compatible while the generated net481 callers are
            // staged. The service rejects a zero tuple before state mutation.
            if (request.AccountId < 0)
            {
                return CommunicationContractValidationError.InvalidAccountId;
            }
            if (request.SessionId < 0)
            {
                return CommunicationContractValidationError.InvalidSessionId;
            }

            return request.CharacterId > 0
                ? CommunicationContractValidationError.None
                : CommunicationContractValidationError.InvalidCharacterId;
        }

        private static CommunicationContractValidationError ValidateRequest(
            object request,
            WireV1.RequestContext context,
            params ClusterNodeRole[] allowedRoles)
        {
            if (request == null)
            {
                return CommunicationContractValidationError.MissingRequest;
            }

            return ValidateContext(context, allowedRoles);
        }

        private static CommunicationContractValidationError ValidateContext(
            WireV1.RequestContext context,
            params ClusterNodeRole[] allowedRoles)
        {
            if (context?.Version == null ||
                context.Version.Major > ushort.MaxValue ||
                context.Version.Minor > ushort.MaxValue)
            {
                return CommunicationContractValidationError.InvalidContext;
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
                return CommunicationContractValidationError.InvalidContext;
            }

            return allowedRoles.Contains(contractContext.CallerRole)
                ? CommunicationContractValidationError.None
                : CommunicationContractValidationError.InvalidCallerRole;
        }

        private static CommunicationContractValidationError ValidateAccountSession(
            long accountId,
            int sessionId)
        {
            CommunicationContractValidationError error =
                ValidateAccountId(accountId);
            if (error != CommunicationContractValidationError.None)
            {
                return error;
            }

            return sessionId > 0
                ? CommunicationContractValidationError.None
                : CommunicationContractValidationError.InvalidSessionId;
        }

        private static CommunicationContractValidationError ValidateAccountId(
            long accountId)
        {
            return accountId > 0
                ? CommunicationContractValidationError.None
                : CommunicationContractValidationError.InvalidAccountId;
        }

        private static CommunicationContractValidationError ValidateWorldId(
            string worldId)
        {
            return worldId != null &&
                   worldId.Length == 36 &&
                   Guid.TryParseExact(worldId, "D", out Guid parsedWorldId) &&
                   parsedWorldId != Guid.Empty
                ? CommunicationContractValidationError.None
                : CommunicationContractValidationError.InvalidWorldId;
        }

        private static CommunicationContractValidationError ValidateIpAddress(
            string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress) ||
                ipAddress.Length > CommunicationContractLimits.MaxIpAddressLength ||
                !IPAddress.TryParse(ipAddress, out IPAddress parsedAddress) ||
                !string.Equals(
                    parsedAddress.ToString(),
                    ipAddress,
                    StringComparison.OrdinalIgnoreCase))
            {
                return CommunicationContractValidationError.InvalidIpAddress;
            }

            return CommunicationContractValidationError.None;
        }

        private static bool IsBoundedText(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Length > maximumLength ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                return false;
            }

            return value.All(character =>
                char.GetUnicodeCategory(character) != UnicodeCategory.Control);
        }
    }
}
