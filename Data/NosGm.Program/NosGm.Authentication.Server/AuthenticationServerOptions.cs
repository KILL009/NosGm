using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using NosGm.Cluster.Contracts.V1;
using WireNodeRole = NosGm.Cluster.Wire.V1.ClusterNodeRole;

namespace NosGm.Authentication.Server;

public sealed class AuthenticationServerOptions
{
    public const string PortVariable = "NOSGM_AUTH_GRPC_PORT";
    public const string CertificatePathVariable =
        "NOSGM_AUTH_GRPC_SERVER_CERT_PATH";
    public const string CertificatePasswordVariable =
        "NOSGM_AUTH_GRPC_SERVER_CERT_PASSWORD";
    public const string TrustedRootCertificatePathVariable =
        "NOSGM_AUTH_GRPC_TRUSTED_ROOT_CERT_PATH";
    public const string AuthBridgeFingerprintsVariable =
        "NOSGM_AUTH_GRPC_AUTHBRIDGE_CERT_SHA256";
    public const string LoginFingerprintsVariable =
        "NOSGM_AUTH_GRPC_LOGIN_CERT_SHA256";
    public const string WorldFingerprintsVariable =
        "NOSGM_AUTH_GRPC_WORLD_CERT_SHA256";
    public const string TicketTtlVariable =
        "NOSGM_AUTH_GRPC_TICKET_TTL_SECONDS";
    public const string PermitTtlVariable =
        "NOSGM_AUTH_GRPC_PERMIT_TTL_SECONDS";
    public const string InstanceIdVariable =
        "NOSGM_AUTH_GRPC_INSTANCE_ID";

    public const int DefaultPort = 7443;
    public const int DefaultTtlSeconds = 120;
    public const int MinimumTtlSeconds = 15;
    public const int MaximumTtlSeconds = 600;
    public const int MaximumReplayEntries = 10000;

    private AuthenticationServerOptions(
        int port,
        string certificatePath,
        string certificatePassword,
        string trustedRootCertificatePath,
        int ticketTtlSeconds,
        int permitTtlSeconds,
        string instanceId,
        IReadOnlyDictionary<WireNodeRole, IReadOnlyCollection<string>>
            allowedFingerprints)
    {
        Port = port;
        CertificatePath = certificatePath;
        CertificatePassword = certificatePassword;
        TrustedRootCertificatePath = trustedRootCertificatePath;
        TicketTtlSeconds = ticketTtlSeconds;
        PermitTtlSeconds = permitTtlSeconds;
        InstanceId = instanceId;
        AllowedFingerprints = allowedFingerprints;
    }

    public int Port { get; }

    public string CertificatePath { get; }

    public string CertificatePassword { get; }

    public string TrustedRootCertificatePath { get; }

    public int TicketTtlSeconds { get; }

    public int PermitTtlSeconds { get; }

    public string InstanceId { get; }

    public IReadOnlyDictionary<WireNodeRole, IReadOnlyCollection<string>>
        AllowedFingerprints { get; }

    public static AuthenticationServerOptions Load(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        int port = ReadInteger(
            configuration[PortVariable],
            DefaultPort,
            1024,
            65535,
            PortVariable);
        int ticketTtl = ReadInteger(
            configuration[TicketTtlVariable],
            DefaultTtlSeconds,
            MinimumTtlSeconds,
            MaximumTtlSeconds,
            TicketTtlVariable);
        int permitTtl = ReadInteger(
            configuration[PermitTtlVariable],
            DefaultTtlSeconds,
            MinimumTtlSeconds,
            MaximumTtlSeconds,
            PermitTtlVariable);

        string certificatePath = ReadRequiredText(
            configuration[CertificatePathVariable],
            CertificatePathVariable,
            1024);
        if (!Path.IsPathFullyQualified(certificatePath))
        {
            throw new InvalidOperationException(
                CertificatePathVariable + " must be an absolute path.");
        }

        string certificatePassword = ReadOptionalSecret(
            configuration[CertificatePasswordVariable],
            CertificatePasswordVariable);
        string trustedRootCertificatePath = ReadOptionalAbsolutePath(
            configuration[TrustedRootCertificatePathVariable],
            TrustedRootCertificatePathVariable);
        string instanceId = configuration[InstanceIdVariable];
        if (string.IsNullOrEmpty(instanceId))
        {
            instanceId = "authentication-local-1";
        }
        else
        {
            instanceId = ReadRequiredText(
                instanceId,
                InstanceIdVariable,
                ClusterProtocolLimits.MaxCallerInstanceIdLength);
        }

        var roles =
            new Dictionary<WireNodeRole, IReadOnlyCollection<string>>
            {
                [WireNodeRole.AuthBridge] = ParseFingerprints(
                    configuration[AuthBridgeFingerprintsVariable],
                    AuthBridgeFingerprintsVariable),
                [WireNodeRole.Login] = ParseFingerprints(
                    configuration[LoginFingerprintsVariable],
                    LoginFingerprintsVariable),
                [WireNodeRole.World] = ParseFingerprints(
                    configuration[WorldFingerprintsVariable],
                    WorldFingerprintsVariable)
            };
        RejectCrossRoleCertificateReuse(roles);

        return new AuthenticationServerOptions(
            port,
            certificatePath,
            certificatePassword,
            trustedRootCertificatePath,
            ticketTtl,
            permitTtl,
            instanceId,
            roles);
    }

    public X509Certificate2 LoadServerCertificate()
    {
        if (!File.Exists(CertificatePath))
        {
            throw new InvalidOperationException(
                "The authentication server certificate file does not exist.");
        }

        X509Certificate2 certificate =
            X509CertificateLoader.LoadPkcs12FromFile(
                CertificatePath,
                CertificatePassword,
                X509KeyStorageFlags.UserKeySet);
        if (!certificate.HasPrivateKey)
        {
            certificate.Dispose();
            throw new InvalidOperationException(
                "The authentication server certificate has no private key.");
        }

        return certificate;
    }

    public X509Certificate2 LoadTrustedRootCertificate()
    {
        if (string.IsNullOrEmpty(TrustedRootCertificatePath))
        {
            return null;
        }
        if (!File.Exists(TrustedRootCertificatePath))
        {
            throw new InvalidOperationException(
                "The authentication trusted-root certificate file does not exist.");
        }

        return X509CertificateLoader.LoadCertificateFromFile(
            TrustedRootCertificatePath);
    }

    private static int ReadInteger(
        string value,
        int defaultValue,
        int minimum,
        int maximum,
        string variableName)
    {
        if (value == null)
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
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > maximumLength ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
            ContainsControlCharacter(value))
        {
            throw new InvalidOperationException(
                variableName + " contains an invalid value.");
        }

        return value;
    }

    private static string ReadOptionalSecret(
        string value,
        string variableName)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value.Length > 4096 ||
            ContainsControlCharacter(value) ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                variableName + " contains an invalid secret value.");
        }

        return value;
    }

    private static string ReadOptionalAbsolutePath(
        string value,
        string variableName)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        value = ReadRequiredText(value, variableName, 1024);
        if (!Path.IsPathFullyQualified(value))
        {
            throw new InvalidOperationException(
                variableName + " must be an absolute path.");
        }

        return value;
    }

    private static IReadOnlyCollection<string> ParseFingerprints(
        string value,
        string variableName)
    {
        value = ReadRequiredText(value, variableName, 8192);
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        foreach (string candidate in value.Split(','))
        {
            string normalized = candidate.Replace(":", string.Empty);
            if (normalized.Length != 64 ||
                normalized.Any(character => !Uri.IsHexDigit(character)))
            {
                throw new InvalidOperationException(
                    variableName +
                    " must contain comma-separated SHA-256 fingerprints.");
            }

            fingerprints.Add(normalized.ToUpperInvariant());
        }

        return fingerprints.ToArray();
    }

    private static void RejectCrossRoleCertificateReuse(
        IReadOnlyDictionary<WireNodeRole, IReadOnlyCollection<string>> roles)
    {
        var owners = new Dictionary<string, WireNodeRole>(
            StringComparer.Ordinal);
        foreach (KeyValuePair<
                     WireNodeRole,
                     IReadOnlyCollection<string>> role in roles)
        {
            foreach (string fingerprint in role.Value)
            {
                if (owners.TryGetValue(
                        fingerprint,
                        out WireNodeRole existingRole) &&
                    existingRole != role.Key)
                {
                    throw new InvalidOperationException(
                        "A client certificate fingerprint cannot own multiple roles.");
                }

                owners[fingerprint] = role.Key;
            }
        }
    }

    private static bool ContainsControlCharacter(string value)
    {
        return value.Any(char.IsControl);
    }
}
