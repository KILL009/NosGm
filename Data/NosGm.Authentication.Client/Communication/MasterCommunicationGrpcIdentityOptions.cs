using System;
using NosGm.Authentication.Client;
using NosGm.Cluster.Contracts.V1;

namespace NosGm.Communication.Client
{
    /// <summary>
    /// Loads the credential namespace reserved for Master callback publication.
    /// These variables are intentionally separate from the AuthBridge credential
    /// namespace used by the same legacy Master process.
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
            return AuthenticationGrpcClientOptions.Load(
                ClusterNodeRole.Master,
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
