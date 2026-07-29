using System;
using System.Globalization;
using System.Net;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Cluster.Contracts.Authentication.V1
{
    public static class GameforgeAuthenticationContractValidator
    {
        public static AuthenticationContractValidationError Validate(
            WireV1.IssueAuthTicketRequest request)
        {
            AuthenticationContractValidationError contextError =
                ValidateContext(request?.Context, ClusterNodeRole.AuthBridge);
            if (contextError != AuthenticationContractValidationError.None)
            {
                return contextError;
            }

            if (!IsBoundedText(
                    request.AccountName,
                    AuthenticationContractLimits.MaxAccountNameLength))
            {
                return AuthenticationContractValidationError.InvalidAccountName;
            }

            AuthenticationContractValidationError ticketError =
                ValidateTicketBinding(
                    request.AuthorizationCode,
                    request.InstallationId,
                    request.CountryId);
            return ticketError;
        }

        public static AuthenticationContractValidationError Validate(
            WireV1.ConsumeAuthTicketRequest request)
        {
            AuthenticationContractValidationError contextError =
                ValidateContext(request?.Context, ClusterNodeRole.Login);
            if (contextError != AuthenticationContractValidationError.None)
            {
                return contextError;
            }

            AuthenticationContractValidationError ticketError =
                ValidateTicketBinding(
                    request.AuthorizationCode,
                    request.InstallationId,
                    request.CountryId);
            if (ticketError != AuthenticationContractValidationError.None)
            {
                return ticketError;
            }

            return request.ProposedSessionId > 0
                ? AuthenticationContractValidationError.None
                : AuthenticationContractValidationError.InvalidSessionId;
        }

        public static AuthenticationContractValidationError Validate(
            WireV1.IssueWorldPermitRequest request)
        {
            AuthenticationContractValidationError contextError =
                ValidateContext(request?.Context, ClusterNodeRole.Login);
            return contextError != AuthenticationContractValidationError.None
                ? contextError
                : ValidateWorldPermit(
                    request.AccountId,
                    request.SessionId,
                    request.IpAddress);
        }

        public static AuthenticationContractValidationError Validate(
            WireV1.ConsumeWorldPermitRequest request)
        {
            AuthenticationContractValidationError contextError =
                ValidateContext(request?.Context, ClusterNodeRole.World);
            return contextError != AuthenticationContractValidationError.None
                ? contextError
                : ValidateWorldPermit(
                    request.AccountId,
                    request.SessionId,
                    request.IpAddress);
        }

        public static AuthenticationContractValidationError Validate(
            WireV1.RevokeWorldPermitRequest request)
        {
            AuthenticationContractValidationError contextError =
                ValidateContext(request?.Context, ClusterNodeRole.Login);
            if (contextError != AuthenticationContractValidationError.None)
            {
                return contextError;
            }

            if (request.AccountId <= 0)
            {
                return AuthenticationContractValidationError.InvalidAccountId;
            }

            return request.SessionId > 0
                ? AuthenticationContractValidationError.None
                : AuthenticationContractValidationError.InvalidSessionId;
        }

        private static AuthenticationContractValidationError ValidateContext(
            WireV1.RequestContext context,
            ClusterNodeRole expectedRole)
        {
            if (context?.Version == null ||
                context.Version.Major > ushort.MaxValue ||
                context.Version.Minor > ushort.MaxValue)
            {
                return AuthenticationContractValidationError.InvalidContext;
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
                contractContext.RequestedService !=
                ClusterService.Authentication)
            {
                return AuthenticationContractValidationError.InvalidContext;
            }

            return contractContext.CallerRole == expectedRole
                ? AuthenticationContractValidationError.None
                : AuthenticationContractValidationError.InvalidCallerRole;
        }

        private static AuthenticationContractValidationError
            ValidateTicketBinding(
                string authorizationCode,
                string installationId,
                uint countryId)
        {
            if (!IsBoundedText(
                    authorizationCode,
                    AuthenticationContractLimits.MaxAuthorizationCodeLength))
            {
                return AuthenticationContractValidationError
                    .InvalidAuthorizationCode;
            }

            if (!Guid.TryParse(authorizationCode, out _) &&
                !IsSupportedHexAuthorizationCode(authorizationCode))
            {
                return AuthenticationContractValidationError
                    .InvalidAuthorizationCode;
            }

            if (installationId == null ||
                installationId.Length !=
                AuthenticationContractLimits.InstallationIdLength ||
                !Guid.TryParseExact(
                    installationId,
                    "D",
                    out Guid parsedInstallationId) ||
                parsedInstallationId == Guid.Empty)
            {
                return AuthenticationContractValidationError
                    .InvalidInstallationId;
            }

            return countryId <= AuthenticationContractLimits.MaxCountryId
                ? AuthenticationContractValidationError.None
                : AuthenticationContractValidationError.InvalidCountryId;
        }

        private static AuthenticationContractValidationError
            ValidateWorldPermit(
                long accountId,
                int sessionId,
                string ipAddress)
        {
            if (accountId <= 0)
            {
                return AuthenticationContractValidationError.InvalidAccountId;
            }

            if (sessionId <= 0)
            {
                return AuthenticationContractValidationError.InvalidSessionId;
            }

            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return AuthenticationContractValidationError.None;
            }

            if (ipAddress.Length >
                AuthenticationContractLimits.MaxIpAddressLength ||
                !IPAddress.TryParse(ipAddress, out IPAddress parsedAddress) ||
                !string.Equals(
                    parsedAddress.ToString(),
                    ipAddress,
                    StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticationContractValidationError.InvalidIpAddress;
            }

            return AuthenticationContractValidationError.None;
        }

        private static bool IsBoundedText(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Length > maximumLength)
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

        private static bool IsSupportedHexAuthorizationCode(string value)
        {
            if (value.Length < 32 || value.Length % 2 != 0)
            {
                return false;
            }

            foreach (char character in value)
            {
                bool isHex =
                    character >= '0' && character <= '9' ||
                    character >= 'a' && character <= 'f' ||
                    character >= 'A' && character <= 'F';
                if (!isHex)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
