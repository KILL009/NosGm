using System;
using System.Globalization;
using System.IO;
using NosGm.Cluster.Contracts.V1;

namespace NosGm.Authentication.Client
{
    public sealed class AuthenticationGrpcClientOptions
    {
        public const string AddressVariable = "NOSGM_AUTH_GRPC_URL";
        public const string CertificatePathVariable =
            "NOSGM_AUTH_GRPC_CLIENT_CERT_PATH";
        public const string CertificatePasswordVariable =
            "NOSGM_AUTH_GRPC_CLIENT_CERT_PASSWORD";
        public const string TrustedRootCertificatePathVariable =
            "NOSGM_AUTH_GRPC_TRUSTED_ROOT_CERT_PATH";
        public const string CallerInstanceIdVariable =
            "NOSGM_AUTH_GRPC_CALLER_INSTANCE_ID";
        public const string DeadlineVariable =
            "NOSGM_AUTH_GRPC_DEADLINE_MILLISECONDS";
        public const string WireModeVariable =
            "NOSGM_AUTH_GRPC_WIRE_MODE";

        public const string DefaultAddress = "https://127.0.0.1:7443";
        public const int MinimumDeadlineMilliseconds = 1000;

        private AuthenticationGrpcClientOptions(
            Uri address,
            string certificatePath,
            string certificatePassword,
            string trustedRootCertificatePath,
            string callerInstanceId,
            int deadlineMilliseconds,
            ClusterNodeRole callerRole,
            AuthenticationGrpcWireMode wireMode)
        {
            Address = address;
            CertificatePath = certificatePath;
            CertificatePassword = certificatePassword;
            TrustedRootCertificatePath = trustedRootCertificatePath;
            CallerInstanceId = callerInstanceId;
            DeadlineMilliseconds = deadlineMilliseconds;
            CallerRole = callerRole;
            WireMode = wireMode;
        }

        public Uri Address { get; }

        public string CertificatePath { get; }

        public string CertificatePassword { get; }

        public string TrustedRootCertificatePath { get; }

        public string CallerInstanceId { get; }

        public int DeadlineMilliseconds { get; }

        public ClusterNodeRole CallerRole { get; }

        public AuthenticationGrpcWireMode WireMode { get; }

        public static AuthenticationGrpcClientOptions Load(
            ClusterNodeRole callerRole,
            Func<string, string> readVariable = null)
        {
            if (callerRole != ClusterNodeRole.AuthBridge &&
                callerRole != ClusterNodeRole.Login &&
                callerRole != ClusterNodeRole.World)
            {
                throw new InvalidOperationException(
                    "The authentication gRPC client role must be AuthBridge, Login, or World.");
            }

            return LoadCore(callerRole, readVariable);
        }

        internal static AuthenticationGrpcClientOptions LoadMaster(
            Func<string, string> readVariable)
        {
            return LoadCore(ClusterNodeRole.Master, readVariable);
        }

        private static AuthenticationGrpcClientOptions LoadCore(
            ClusterNodeRole callerRole,
            Func<string, string> readVariable)
        {
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

            string certificatePath = ReadBoundedValue(
                readVariable(CertificatePathVariable),
                CertificatePathVariable,
                1024,
                required: true);
            if (!Path.IsPathRooted(certificatePath))
            {
                throw new InvalidOperationException(
                    CertificatePathVariable + " must be an absolute path.");
            }

            string certificatePassword = ReadBoundedValue(
                readVariable(CertificatePasswordVariable),
                CertificatePasswordVariable,
                4096,
                required: false);
            string trustedRootCertificatePath = ReadBoundedValue(
                readVariable(TrustedRootCertificatePathVariable),
                TrustedRootCertificatePathVariable,
                1024,
                required: false);
            if (!string.IsNullOrEmpty(trustedRootCertificatePath) &&
                !Path.IsPathRooted(trustedRootCertificatePath))
            {
                throw new InvalidOperationException(
                    TrustedRootCertificatePathVariable +
                    " must be an absolute path.");
            }
#if !NET10_0_OR_GREATER
            if (!string.IsNullOrEmpty(trustedRootCertificatePath))
            {
                throw new InvalidOperationException(
                    TrustedRootCertificatePathVariable +
                    " is reserved for the isolated .NET 10 acceptance process.");
            }
#endif
            string callerInstanceId = ReadBoundedValue(
                readVariable(CallerInstanceIdVariable),
                CallerInstanceIdVariable,
                ClusterProtocolLimits.MaxCallerInstanceIdLength,
                required: true);
            int deadlineMilliseconds = ReadDeadline(
                readVariable(DeadlineVariable));
            AuthenticationGrpcWireMode wireMode = ReadWireMode(
                readVariable(WireModeVariable));

            return new AuthenticationGrpcClientOptions(
                address,
                certificatePath,
                certificatePassword,
                trustedRootCertificatePath,
                callerInstanceId,
                deadlineMilliseconds,
                callerRole,
                wireMode);
        }

        private static AuthenticationGrpcWireMode ReadWireMode(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                string.Equals(
                    value,
                    "HTTP2",
                    StringComparison.OrdinalIgnoreCase))
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

        private static int ReadDeadline(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return ClusterProtocolLimits.DefaultDeadlineMilliseconds;
            }

            if (!int.TryParse(
                    value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int parsed) ||
                parsed < MinimumDeadlineMilliseconds ||
                parsed > ClusterProtocolLimits.MaxDeadlineMilliseconds)
            {
                throw new InvalidOperationException(
                    DeadlineVariable +
                    " must be an integer between " +
                    MinimumDeadlineMilliseconds +
                    " and " +
                    ClusterProtocolLimits.MaxDeadlineMilliseconds +
                    ".");
            }

            return parsed;
        }

        private static string ReadBoundedValue(
            string value,
            string variableName,
            int maximumLength,
            bool required)
        {
            if (!required && string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(value) ||
                value.Length > maximumLength ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    variableName + " contains an invalid value.");
            }

            foreach (char character in value)
            {
                if (char.IsControl(character))
                {
                    throw new InvalidOperationException(
                        variableName + " contains an invalid value.");
                }
            }

            return value;
        }
    }
}
