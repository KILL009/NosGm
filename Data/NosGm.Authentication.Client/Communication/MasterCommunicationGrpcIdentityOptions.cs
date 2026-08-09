using System;
using NosGm.Authentication.Client;
using NosGm.Authentication.Client.Configuration;

namespace NosGm.Communication.Client
{
    /// <summary>
    /// Loads the credential namespace reserved for Master callback publication.
    /// Dedicated callback variables remain authoritative. The explicit local
    /// shadow fallback may reuse the already role-scoped Master Configuration
    /// identity so the normal stack can exercise callback publication without
    /// introducing another plaintext credential boundary.
    /// </summary>
    public static class MasterCommunicationGrpcIdentityOptions
    {
        public const string AddressVariable =
            "NOSGM_COMMUNICATION_GRPC_URL";
        public const string CertificatePathVariable =
            "NOSGM_COMMUNICATION_GRPC_MASTER_CERT_PATH";
        public const string CertificatePasswordVariable =
            "NOSGM_COMMUNICATION_GRPC_MASTER_CERT_PASSWORD";
        public const string TrustedRootCertificatePathVariable =
            "NOSGM_COMMUNICATION_GRPC_TRUSTED_ROOT_CERT_PATH";
        public const string CallerInstanceIdVariable =
            "NOSGM_COMMUNICATION_GRPC_MASTER_INSTANCE_ID";
        public const string DeadlineVariable =
            "NOSGM_COMMUNICATION_GRPC_DEADLINE_MILLISECONDS";
        public const string WireModeVariable =
            "NOSGM_COMMUNICATION_GRPC_WIRE_MODE";

        public static AuthenticationGrpcClientOptions Load(
            Func<string, string> readVariable = null)
        {
            readVariable = readVariable ?? Environment.GetEnvironmentVariable;
            bool useExistingIdentity =
                CommunicationCallbackExistingIdentityFallback.IsEnabled(
                    readVariable);

            string ReadValue(string dedicatedVariable, string fallbackVariable)
            {
                string dedicated = readVariable(dedicatedVariable);
                if (!string.IsNullOrEmpty(dedicated) || !useExistingIdentity)
                {
                    return dedicated;
                }
                return readVariable(fallbackVariable);
            }

            return AuthenticationGrpcClientOptions.LoadMaster(
                genericVariable => genericVariable switch
                {
                    AuthenticationGrpcClientOptions.AddressVariable =>
                        ReadValue(
                            AddressVariable,
                            ConfigurationRuntimeControllerIdentityOptions
                                .AddressVariable),
                    AuthenticationGrpcClientOptions.CertificatePathVariable =>
                        ReadValue(
                            CertificatePathVariable,
                            ConfigurationRuntimeControllerIdentityOptions
                                .CertificatePathVariable),
                    AuthenticationGrpcClientOptions.CertificatePasswordVariable =>
                        ReadValue(
                            CertificatePasswordVariable,
                            ConfigurationRuntimeControllerIdentityOptions
                                .CertificatePasswordVariable),
                    AuthenticationGrpcClientOptions
                            .TrustedRootCertificatePathVariable =>
                        ReadValue(
                            TrustedRootCertificatePathVariable,
                            ConfigurationRuntimeControllerIdentityOptions
                                .TrustedRootCertificatePathVariable),
                    AuthenticationGrpcClientOptions.CallerInstanceIdVariable =>
                        ReadValue(
                            CallerInstanceIdVariable,
                            ConfigurationRuntimeControllerIdentityOptions
                                .CallerInstanceIdVariable),
                    AuthenticationGrpcClientOptions.DeadlineVariable =>
                        ReadValue(
                            DeadlineVariable,
                            ConfigurationRuntimeControllerIdentityOptions
                                .DeadlineVariable),
                    AuthenticationGrpcClientOptions.WireModeVariable =>
                        ReadValue(
                            WireModeVariable,
                            ConfigurationRuntimeControllerIdentityOptions
                                .WireModeVariable),
                    _ => null
                });
        }
    }
}
