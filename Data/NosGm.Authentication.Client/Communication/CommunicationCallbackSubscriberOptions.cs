using System;
using System.Globalization;
using System.IO;
using NosGm.Authentication.Client;
using NosGm.Cluster.Contracts.Communication.V1;
using NosGm.Cluster.Contracts.V1;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationCallbackSubscriberOptions
    {
        public const string AddressVariable =
            "NOSGM_COMMUNICATION_GRPC_URL";
        public const string CertificatePathVariable =
            "NOSGM_COMMUNICATION_GRPC_CLIENT_CERT_PATH";
        public const string CertificatePasswordVariable =
            "NOSGM_COMMUNICATION_GRPC_CLIENT_CERT_PASSWORD";
        public const string TrustedRootCertificatePathVariable =
            "NOSGM_COMMUNICATION_GRPC_TRUSTED_ROOT_CERT_PATH";
        public const string CallerInstanceIdVariable =
            "NOSGM_COMMUNICATION_GRPC_CALLER_INSTANCE_ID";
        public const string SetupDeadlineVariable =
            "NOSGM_COMMUNICATION_GRPC_SETUP_DEADLINE_MILLISECONDS";
        public const string WireModeVariable =
            "NOSGM_COMMUNICATION_GRPC_WIRE_MODE";
        public const string CursorPathVariable =
            "NOSGM_COMMUNICATION_GRPC_CALLBACK_CURSOR_PATH";
        public const string InitialReconnectDelayVariable =
            "NOSGM_COMMUNICATION_GRPC_CALLBACK_RECONNECT_INITIAL_MILLISECONDS";
        public const string MaximumReconnectDelayVariable =
            "NOSGM_COMMUNICATION_GRPC_CALLBACK_RECONNECT_MAXIMUM_MILLISECONDS";

        public const string DefaultAddress = "https://127.0.0.1:7443";
        public const int DefaultSetupDeadlineMilliseconds = 10000;
        public const int DefaultInitialReconnectDelayMilliseconds = 1000;
        public const int DefaultMaximumReconnectDelayMilliseconds = 30000;

        private CommunicationCallbackSubscriberOptions(
            Uri address,
            string certificatePath,
            string certificatePassword,
            string trustedRootCertificatePath,
            string callerInstanceId,
            string cursorPath,
            int setupDeadlineMilliseconds,
            int initialReconnectDelayMilliseconds,
            int maximumReconnectDelayMilliseconds,
            ClusterNodeRole callerRole,
            AuthenticationGrpcWireMode wireMode,
            Guid worldId,
            int channelId,
            string worldGroup)
        {
            Address = address;
            CertificatePath = certificatePath;
            CertificatePassword = certificatePassword;
            TrustedRootCertificatePath = trustedRootCertificatePath;
            CallerInstanceId = callerInstanceId;
            CursorPath = cursorPath;
            SetupDeadlineMilliseconds = setupDeadlineMilliseconds;
            InitialReconnectDelayMilliseconds =
                initialReconnectDelayMilliseconds;
            MaximumReconnectDelayMilliseconds =
                maximumReconnectDelayMilliseconds;
            CallerRole = callerRole;
            WireMode = wireMode;
            WorldId = worldId;
            ChannelId = channelId;
            WorldGroup = worldGroup;
        }

        public Uri Address { get; }

        public string CertificatePath { get; }

        public string CertificatePassword { get; }

        public string TrustedRootCertificatePath { get; }

        public string CallerInstanceId { get; }

        public string CursorPath { get; }

        public int SetupDeadlineMilliseconds { get; }

        public int InitialReconnectDelayMilliseconds { get; }

        public int MaximumReconnectDelayMilliseconds { get; }

        public ClusterNodeRole CallerRole { get; }

        public AuthenticationGrpcWireMode WireMode { get; }

        public Guid WorldId { get; }

        public int ChannelId { get; }

        public string WorldGroup { get; }

        public static CommunicationCallbackSubscriberOptions Load(
            ClusterNodeRole callerRole,
            Guid worldId,
            int channelId,
            string worldGroup,
            Func<string, string> readVariable = null)
        {
            if (callerRole != ClusterNodeRole.Login &&
                callerRole != ClusterNodeRole.World)
            {
                throw new InvalidOperationException(
                    "The callback subscriber role must be Login or World.");
            }

            if (callerRole == ClusterNodeRole.World)
            {
                if (worldId == Guid.Empty ||
                    channelId <= 0 ||
                    !IsValidText(
                        worldGroup,
                        CommunicationCallbackContractLimits
                            .MaxWorldGroupLength))
                {
                    throw new InvalidOperationException(
                        "World callback identity is incomplete.");
                }
            }
            else if (worldId != Guid.Empty ||
                     channelId != 0 ||
                     !string.IsNullOrEmpty(worldGroup))
            {
                throw new InvalidOperationException(
                    "Login callback identity cannot contain World fields.");
            }

            readVariable = readVariable ?? Environment.GetEnvironmentVariable;
            string configuredAddress = readVariable(AddressVariable);
            if (string.IsNullOrEmpty(configuredAddress))
            {
                configuredAddress = DefaultAddress;
            }
            if (!Uri.TryCreate(
                    configuredAddress,
                    UriKind.Absolute,
                    out Uri address) ||
                !string.Equals(
                    address.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase) ||
                !address.IsLoopback ||
                !string.IsNullOrEmpty(address.UserInfo) ||
                !string.IsNullOrEmpty(address.Query) ||
                !string.IsNullOrEmpty(address.Fragment) ||
                !string.Equals(address.AbsolutePath, "/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    AddressVariable +
                    " must be an HTTPS loopback origin without credentials, path, query, or fragment.");
            }

            string certificatePath = ReadRequiredText(
                readVariable(CertificatePathVariable),
                CertificatePathVariable,
                1024);
            string cursorPath = ReadRequiredText(
                readVariable(CursorPathVariable),
                CursorPathVariable,
                1024);
            if (!Path.IsPathRooted(certificatePath) ||
                !Path.IsPathRooted(cursorPath))
            {
                throw new InvalidOperationException(
                    "Callback certificate and cursor paths must be absolute.");
            }
            if (string.Equals(
                    Path.GetFullPath(certificatePath),
                    Path.GetFullPath(cursorPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The callback cursor path cannot overwrite the client certificate.");
            }

            string trustedRootPath = ReadOptionalText(
                readVariable(TrustedRootCertificatePathVariable),
                TrustedRootCertificatePathVariable,
                1024);
            if (!string.IsNullOrEmpty(trustedRootPath) &&
                !Path.IsPathRooted(trustedRootPath))
            {
                throw new InvalidOperationException(
                    TrustedRootCertificatePathVariable +
                    " must be an absolute path.");
            }
#if !NET10_0_OR_GREATER
            if (!string.IsNullOrEmpty(trustedRootPath))
            {
                throw new InvalidOperationException(
                    TrustedRootCertificatePathVariable +
                    " is reserved for the isolated .NET 10 acceptance process.");
            }
#endif
            string certificatePassword = ReadOptionalText(
                readVariable(CertificatePasswordVariable),
                CertificatePasswordVariable,
                4096);
            string callerInstanceId = ReadRequiredText(
                readVariable(CallerInstanceIdVariable),
                CallerInstanceIdVariable,
                ClusterProtocolLimits.MaxCallerInstanceIdLength);
            int setupDeadline = ReadInteger(
                readVariable(SetupDeadlineVariable),
                DefaultSetupDeadlineMilliseconds,
                1000,
                ClusterProtocolLimits.MaxDeadlineMilliseconds,
                SetupDeadlineVariable);
            int initialReconnect = ReadInteger(
                readVariable(InitialReconnectDelayVariable),
                DefaultInitialReconnectDelayMilliseconds,
                100,
                60000,
                InitialReconnectDelayVariable);
            int maximumReconnect = ReadInteger(
                readVariable(MaximumReconnectDelayVariable),
                DefaultMaximumReconnectDelayMilliseconds,
                initialReconnect,
                300000,
                MaximumReconnectDelayVariable);
            AuthenticationGrpcWireMode wireMode = ReadWireMode(
                readVariable(WireModeVariable));

            return new CommunicationCallbackSubscriberOptions(
                address,
                Path.GetFullPath(certificatePath),
                certificatePassword,
                string.IsNullOrEmpty(trustedRootPath)
                    ? string.Empty
                    : Path.GetFullPath(trustedRootPath),
                callerInstanceId,
                Path.GetFullPath(cursorPath),
                setupDeadline,
                initialReconnect,
                maximumReconnect,
                callerRole,
                wireMode,
                worldId,
                channelId,
                worldGroup ?? string.Empty);
        }

        private static AuthenticationGrpcWireMode ReadWireMode(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                string.Equals(value, "HTTP2", StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticationGrpcWireMode.Http2;
            }
            if (string.Equals(
                    value,
                    "GRPCWEB",
                    StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticationGrpcWireMode.GrpcWeb;
            }

            throw new InvalidOperationException(
                WireModeVariable + " must be HTTP2 or GRPCWEB.");
        }

        private static int ReadInteger(
            string value,
            int defaultValue,
            int minimum,
            int maximum,
            string variableName)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            if (!int.TryParse(
                    value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int parsed) ||
                parsed < minimum ||
                parsed > maximum)
            {
                throw new InvalidOperationException(
                    variableName +
                    " must be an integer between " +
                    minimum +
                    " and " +
                    maximum +
                    ".");
            }
            return parsed;
        }

        private static string ReadRequiredText(
            string value,
            string variableName,
            int maximumLength)
        {
            if (!IsValidText(value, maximumLength))
            {
                throw new InvalidOperationException(
                    variableName + " contains an invalid value.");
            }
            return value;
        }

        private static string ReadOptionalText(
            string value,
            string variableName,
            int maximumLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return ReadRequiredText(value, variableName, maximumLength);
        }

        private static bool IsValidText(string value, int maximumLength)
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
