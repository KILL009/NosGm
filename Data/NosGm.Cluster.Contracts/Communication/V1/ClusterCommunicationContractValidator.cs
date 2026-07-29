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
            if (request == null)
            {
                return CommunicationContractValidationError.MissingRequest;
            }

            CommunicationContractValidationError contextError = ValidateContext(
                request.Context,
                ClusterNodeRole.Login);
            if (contextError != CommunicationContractValidationError.None)
            {
                return contextError;
            }

            CommunicationContractValidationError sessionError =
                ValidateAccountSession(request.AccountId, request.SessionId);
            return sessionError != CommunicationContractValidationError.None
                ? sessionError
                : ValidateIpAddress(request.IpAddress, false);
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
            if (request == null)
            {
                return CommunicationContractValidationError.MissingRequest;
            }

            CommunicationContractValidationError contextError = ValidateContext(
                request.Context,
                ClusterNodeRole.Login,
                ClusterNodeRole.World);
            return contextError != CommunicationContractValidationError.None
                ? contextError
                : ValidateAccountId(request.AccountId);
        }

        public static CommunicationContractValidationError Validate(
            WireV1.ConnectAccountRequest request)
        {
            if (request == null)
            {
                return CommunicationContractValidationError.MissingRequest;
            }

            CommunicationContractValidationError contextError = ValidateContext(
                request.Context,
                ClusterNodeRole.World);
            if (contextError != CommunicationContractValidationError.None)
            {
                return contextError;
            }

            CommunicationContractValidationError worldError =
                ValidateWorldId(request.WorldId);
            return worldError != CommunicationContractValidationError.None
                ? worldError
                : ValidateAccountSession(request.AccountId, request.SessionId);
        }

        public static CommunicationContractValidationError Validate(
            WireV1.DisconnectAccountRequest request)
        {
            if (request == null)
            {
                return CommunicationContractValidationError.MissingRequest;
            }

            CommunicationContractValidationError contextError = ValidateContext(
                request.Context,
                ClusterNodeRole.Login,
                ClusterNodeRole.World);
            if (contextError != CommunicationContractValidationError.None)
            {
                return contextError;
            }

            CommunicationContractValidationError accountError =
                ValidateAccountId(request.AccountId);
            if (accountError != CommunicationContractValidationError.None)
            {
                return accountError;
            }

            if (request.SessionId < 0)
            {
                return CommunicationContractValidationError.InvalidSessionId;
            }

            if (request.PreserveSessionRegistration && request.SessionId <= 0)
            {
                return CommunicationContractValidationError
                    .InvalidPreserveSessionRequest;
            }

            return CommunicationContractValidationError.None;
        }

        public static CommunicationContractValidationError ValidatePulse(
            WireV1.AccountRequest request)
        {
            if (request == null)
            {
                return CommunicationContractValidationError.MissingRequest;
            }

            CommunicationContractValidationError contextError = ValidateContext(
                request.Context,
                ClusterNodeRole.World);
            return contextError != CommunicationContractValidationError.None
                ? contextError
                : ValidateAccountId(request.AccountId);
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
            if (request == null)
            {
                return CommunicationContractValidationError.MissingRequest;
            }

            CommunicationContractValidationError contextError = ValidateContext(
                request.Context,
                ClusterNodeRole.World);
            if (contextError != CommunicationContractValidationError.None)
            {
                return contextError;
            }

            if (request.World == null)
            {
                return CommunicationContractValidationError
                    .MissingWorldRegistration;
            }

            CommunicationContractValidationError worldError =
                ValidateWorldId(request.World.WorldId);
            if (worldError != CommunicationContractValidationError.None)
            {
                return worldError;
            }

            CommunicationContractValidationError ipError =
                ValidateIpAddress(request.World.EndpointIp, false);
            if (ipError != CommunicationContractValidationError.None)
            {
                return ipError;
            }

            if (request.World.EndpointPort == 0 ||
                request.World.EndpointPort >
                CommunicationContractLimits.MaxEndpointPort)
            {
                return CommunicationContractValidationError
                    .InvalidEndpointPort;
            }

            if (request.World.AccountLimit == 0 ||
                request.World.AccountLimit >
                CommunicationContractLimits.MaxAccountLimit)
            {
                return CommunicationContractValidationError
                    .InvalidAccountLimit;
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
            if (request == null)
            {
                return CommunicationContractValidationError.MissingRequest;
            }

            CommunicationContractValidationError contextError = ValidateContext(
                request.Context,
                ClusterNodeRole.World);
            return contextError != CommunicationContractValidationError.None
                ? contextError
                : ValidateWorldId(request.WorldId);
        }

        public static CommunicationContractValidationError Validate(
            WireV1.ListWorldServersRequest request)
        {
            if (request == null)
            {
                return CommunicationContractValidationError.MissingRequest;
            }

            CommunicationContractValidationError contextError = ValidateContext(
                request.Context,
                ClusterNodeRole.Login);
            return contextError != CommunicationContractValidationError.None
                ? contextError
                : ValidateAccountId(request.AccountId);
        }

        private static CommunicationContractValidationError
            ValidateAccountSessionRequest(
                WireV1.AccountSessionRequest request,
                ClusterNodeRole expectedRole)
        {
            if (request == null)
            {
                return CommunicationContractValidationError.MissingRequest;
            }

            CommunicationContractValidationError contextError = ValidateContext(
                request.Context,
                expectedRole);
            return contextError != CommunicationContractValidationError.None
                ? contextError
                : ValidateAccountSession(request.AccountId, request.SessionId);
        }

        private static CommunicationContractValidationError
            ValidateCharacterWorldRequest(WireV1.CharacterWorldRequest request)
        {
            if (request == null)
            {
                return CommunicationContractValidationError.MissingRequest;
            }

            CommunicationContractValidationError contextError = ValidateContext(
                request.Context,
                ClusterNodeRole.World);
            if (contextError != CommunicationContractValidationError.None)
            {
                return contextError;
            }

            CommunicationContractValidationError worldError =
                ValidateWorldId(request.WorldId);
            if (worldError != CommunicationContractValidationError.None)
            {
                return worldError;
            }

            return request.CharacterId > 0
                ? CommunicationContractValidationError.None
                : CommunicationContractValidationError.InvalidCharacterId;
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

            foreach (ClusterNodeRole allowedRole in allowedRoles)
            {
                if (contractContext.CallerRole == allowedRole)
                {
                    return CommunicationContractValidationError.None;
                }
            }

            return CommunicationContractValidationError.InvalidCallerRole;
        }

        private static CommunicationContractValidationError ValidateAccountSession(
            long accountId,
            int sessionId)
        {
            CommunicationContractValidationError accountError =
                ValidateAccountId(accountId);
            if (accountError != CommunicationContractValidationError.None)
            {
                return accountError;
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
            if (worldId == null ||
                worldId.Length != 36 ||
                !Guid.TryParseExact(worldId, "D", out Guid parsedWorldId) ||
                parsedWorldId == Guid.Empty)
            {
                return CommunicationContractValidationError.InvalidWorldId;
            }

            return CommunicationContractValidationError.None;
        }

        private static CommunicationContractValidationError ValidateIpAddress(
            string ipAddress,
            bool allowEmpty)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return allowEmpty
                    ? CommunicationContractValidationError.None
                    : CommunicationContractValidationError.InvalidIpAddress;
            }

            if (ipAddress.Length > CommunicationContractLimits.MaxIpAddressLength ||
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

            foreach (char character in value)
            {
                if (char.GetUnicodeCategory(character) ==
                    UnicodeCategory.Control)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
