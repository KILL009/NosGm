using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NosGm.Authentication.Client;

namespace NosGm.Communication.Client
{
    /// <summary>
    /// Allows the callback shadow path to reuse the role-separated gRPC identity
    /// that the current process already received from the normal NosGM startup.
    /// The bridge is opt-in and process-local. Dedicated callback variables keep
    /// priority whenever they are supplied explicitly.
    /// </summary>
    public static class CommunicationCallbackExistingIdentityFallback
    {
        public const string EnabledVariable =
            "NOSGM_COMMUNICATION_GRPC_USE_EXISTING_IDENTITY_FALLBACK";

        public static bool IsEnabled(Func<string, string> readVariable = null)
        {
            readVariable = readVariable ?? Environment.GetEnvironmentVariable;
            string value = readVariable(EnabledVariable);
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    EnabledVariable +
                    " must be true or false without surrounding whitespace.");
            }
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            throw new InvalidOperationException(
                EnabledVariable + " must be true or false.");
        }

        public static void PrepareSubscriberEnvironment()
        {
            if (!IsEnabled())
            {
                return;
            }

            CopyIfMissing(
                CommunicationCallbackSubscriberOptions.AddressVariable,
                AuthenticationGrpcClientOptions.AddressVariable);
            CopyIfMissing(
                CommunicationCallbackSubscriberOptions.CertificatePathVariable,
                AuthenticationGrpcClientOptions.CertificatePathVariable);
            CopyIfMissing(
                CommunicationCallbackSubscriberOptions.CertificatePasswordVariable,
                AuthenticationGrpcClientOptions.CertificatePasswordVariable);
            CopyIfMissing(
                CommunicationCallbackSubscriberOptions.CallerInstanceIdVariable,
                AuthenticationGrpcClientOptions.CallerInstanceIdVariable);
            CopyIfMissing(
                CommunicationCallbackSubscriberOptions.SetupDeadlineVariable,
                AuthenticationGrpcClientOptions.DeadlineVariable);
            CopyIfMissing(
                CommunicationCallbackSubscriberOptions.WireModeVariable,
                AuthenticationGrpcClientOptions.WireModeVariable);

            // Do not copy NOSGM_AUTH_GRPC_TRUSTED_ROOT_CERT_PATH into the
            // callback namespace. The legacy net481 subscriber intentionally
            // relies on the CurrentUser Windows trust store, while the isolated
            // .NET 10 acceptance path may opt into file-scoped root pinning.

            if (string.IsNullOrEmpty(
                    Environment.GetEnvironmentVariable(
                        CommunicationCallbackSubscriberOptions.CursorPathVariable)))
            {
                string callerInstanceId = Environment.GetEnvironmentVariable(
                    CommunicationCallbackSubscriberOptions.CallerInstanceIdVariable);
                if (!string.IsNullOrWhiteSpace(callerInstanceId))
                {
                    Environment.SetEnvironmentVariable(
                        CommunicationCallbackSubscriberOptions.CursorPathVariable,
                        BuildFallbackCursorPath(callerInstanceId),
                        EnvironmentVariableTarget.Process);
                }
            }
        }

        private static void CopyIfMissing(string targetVariable, string sourceVariable)
        {
            string existing = Environment.GetEnvironmentVariable(
                targetVariable,
                EnvironmentVariableTarget.Process);
            if (!string.IsNullOrEmpty(existing))
            {
                return;
            }

            string source = Environment.GetEnvironmentVariable(
                sourceVariable,
                EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(source))
            {
                return;
            }

            Environment.SetEnvironmentVariable(
                targetVariable,
                source,
                EnvironmentVariableTarget.Process);
        }

        private static string BuildFallbackCursorPath(string callerInstanceId)
        {
            string localApplicationData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localApplicationData) ||
                !Path.IsPathRooted(localApplicationData))
            {
                throw new InvalidOperationException(
                    "LocalApplicationData is unavailable for the callback cursor fallback.");
            }

            byte[] identityBytes = Encoding.UTF8.GetBytes(callerInstanceId);
            byte[] digest;
            using (SHA256 sha256 = SHA256.Create())
            {
                digest = sha256.ComputeHash(identityBytes);
            }

            string fingerprint = BitConverter
                .ToString(digest)
                .Replace("-", string.Empty)
                .ToLowerInvariant();
            return Path.GetFullPath(
                Path.Combine(
                    localApplicationData,
                    "NosGM",
                    "communication-callback",
                    "cursor-" + fingerprint + ".txt"));
        }
    }
}
