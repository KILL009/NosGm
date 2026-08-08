using System;

namespace NosGm.Authentication.Client.Configuration
{
    public static class ConfigurationRuntimeControllerIdentityOptions
    {
        public const string AddressVariable =
            "NOSGM_CONFIGURATION_GRPC_CONTROL_URL";
        public const string CertificatePathVariable =
            "NOSGM_CONFIGURATION_GRPC_CONTROL_MASTER_CERT_PATH";
        public const string CertificatePasswordVariable =
            "NOSGM_CONFIGURATION_GRPC_CONTROL_MASTER_CERT_PASSWORD";
        public const string TrustedRootCertificatePathVariable =
            "NOSGM_CONFIGURATION_GRPC_CONTROL_TRUSTED_ROOT_CERT_PATH";
        public const string CallerInstanceIdVariable =
            "NOSGM_CONFIGURATION_GRPC_CONTROL_INSTANCE_ID";
        public const string DeadlineVariable =
            "NOSGM_CONFIGURATION_GRPC_CONTROL_DEADLINE_MILLISECONDS";
        public const string WireModeVariable =
            "NOSGM_CONFIGURATION_GRPC_CONTROL_WIRE_MODE";

        public static AuthenticationGrpcClientOptions Load(
            Func<string, string> readVariable = null)
        {
            readVariable = readVariable ??
                Environment.GetEnvironmentVariable;
            return AuthenticationGrpcClientOptions.LoadMaster(
                genericVariable => genericVariable switch
                {
                    AuthenticationGrpcClientOptions.AddressVariable =>
                        readVariable(AddressVariable),
                    AuthenticationGrpcClientOptions.CertificatePathVariable =>
                        readVariable(CertificatePathVariable),
                    AuthenticationGrpcClientOptions.CertificatePasswordVariable =>
                        readVariable(CertificatePasswordVariable),
                    AuthenticationGrpcClientOptions
                            .TrustedRootCertificatePathVariable =>
                        readVariable(TrustedRootCertificatePathVariable),
                    AuthenticationGrpcClientOptions.CallerInstanceIdVariable =>
                        readVariable(CallerInstanceIdVariable),
                    AuthenticationGrpcClientOptions.DeadlineVariable =>
                        readVariable(DeadlineVariable),
                    AuthenticationGrpcClientOptions.WireModeVariable =>
                        readVariable(WireModeVariable),
                    _ => null
                });
        }
    }
}
