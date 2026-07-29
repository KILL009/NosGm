using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

internal static class LocalAuthenticationCertificateGenerator
{
    private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";
    private const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";

    public static string Generate(
        string outputDirectory,
        int keyLength)
    {
        if (keyLength is not (2048 or 3072 or 4096))
        {
            throw new ArgumentOutOfRangeException(
                nameof(keyLength),
                "The RSA key length must be 2048, 3072, or 4096 bits.");
        }

        string outputRoot = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(outputRoot);
        if (Directory.EnumerateFileSystemEntries(outputRoot).Any())
        {
            throw new InvalidOperationException(
                $"The certificate output directory is not empty: {outputRoot}");
        }

        string bundleId = Guid.NewGuid().ToString("N");
        DateTimeOffset notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        DateTimeOffset notAfter = DateTimeOffset.UtcNow.AddYears(2);
        DateTimeOffset rootNotAfter = DateTimeOffset.UtcNow.AddYears(5);

        using RSA rootKey = RSA.Create(keyLength);
        var rootRequest = new CertificateRequest(
            $"CN=NosGM Local Authentication Root {bundleId}",
            rootKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        rootRequest.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                certificateAuthority: true,
                hasPathLengthConstraint: true,
                pathLengthConstraint: 0,
                critical: true));
        rootRequest.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyCertSign |
                X509KeyUsageFlags.CrlSign |
                X509KeyUsageFlags.DigitalSignature,
                critical: true));
        rootRequest.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(
                rootRequest.PublicKey,
                critical: false));

        using X509Certificate2 rootCertificate =
            rootRequest.CreateSelfSigned(notBefore, rootNotAfter);
        string rootCertificatePath =
            Path.Combine(outputRoot, "nosgm-authentication-root.cer");
        File.WriteAllBytes(
            rootCertificatePath,
            rootCertificate.Export(X509ContentType.Cert));

        string serverPassword = NewPassword();
        string serverCertificatePath =
            Path.Combine(outputRoot, "nosgm-authentication-server.pfx");
        CertificateDescriptor server = CreateIssuedCertificate(
            rootCertificate,
            "CN=NosGM Local Authentication Server",
            serverCertificatePath,
            serverPassword,
            keyLength,
            notBefore,
            notAfter,
            ServerAuthenticationOid,
            isServer: true);

        var clients =
            new Dictionary<string, CertificateDescriptor>(
                StringComparer.Ordinal);
        var clientPasswords =
            new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string role in new[] { "AuthBridge", "Login", "World" })
        {
            string password = NewPassword();
            string certificatePath = Path.Combine(
                outputRoot,
                "nosgm-authentication-" +
                role.ToLowerInvariant() +
                ".pfx");
            clients.Add(
                role,
                CreateIssuedCertificate(
                    rootCertificate,
                    $"CN=NosGM Local Authentication {role}",
                    certificatePath,
                    password,
                    keyLength,
                    notBefore,
                    notAfter,
                    ClientAuthenticationOid,
                    isServer: false));
            clientPasswords.Add(role, password);
        }

        string credentialsPath =
            Path.Combine(outputRoot, "credentials.dpapi.clixml");
        string manifestPath = Path.Combine(outputRoot, "manifest.json");
        var manifest = new
        {
            SchemaVersion = 1,
            BundleId = bundleId,
            CreatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
            ExpiresAtUtc = notAfter.ToString("O"),
            RootCertificatePath = rootCertificatePath,
            RootCertificateThumbprint = rootCertificate.Thumbprint,
            ServerCertificatePath = server.CertificatePath,
            ServerCertificateSha256 = server.Sha256,
            Clients = new
            {
                AuthBridge = clients["AuthBridge"],
                Login = clients["Login"],
                World = clients["World"]
            },
            CredentialsPath = credentialsPath
        };
        File.WriteAllText(
            manifestPath,
            JsonSerializer.Serialize(
                manifest,
                new JsonSerializerOptions { WriteIndented = true }));

        return JsonSerializer.Serialize(
            new
            {
                ManifestPath = manifestPath,
                RootCertificatePath = rootCertificatePath,
                RootCertificateThumbprint = rootCertificate.Thumbprint,
                CredentialsPath = credentialsPath,
                Passwords = new
                {
                    Server = serverPassword,
                    AuthBridge = clientPasswords["AuthBridge"],
                    Login = clientPasswords["Login"],
                    World = clientPasswords["World"]
                }
            });
    }

    private static CertificateDescriptor CreateIssuedCertificate(
        X509Certificate2 issuer,
        string subject,
        string certificatePath,
        string password,
        int keyLength,
        DateTimeOffset notBefore,
        DateTimeOffset notAfter,
        string enhancedKeyUsageOid,
        bool isServer)
    {
        using RSA key = RSA.Create(keyLength);
        var request = new CertificateRequest(
            subject,
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                certificateAuthority: false,
                hasPathLengthConstraint: false,
                pathLengthConstraint: 0,
                critical: true));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                isServer
                    ? X509KeyUsageFlags.DigitalSignature |
                      X509KeyUsageFlags.KeyEncipherment
                    : X509KeyUsageFlags.DigitalSignature,
                critical: true));
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection
                {
                    new Oid(enhancedKeyUsageOid)
                },
                critical: true));
        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(
                request.PublicKey,
                critical: false));
        if (isServer)
        {
            var subjectAlternativeName = new SubjectAlternativeNameBuilder();
            subjectAlternativeName.AddDnsName("localhost");
            subjectAlternativeName.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(
                subjectAlternativeName.Build(critical: false));
        }

        byte[] serialNumber = RandomNumberGenerator.GetBytes(16);
        serialNumber[0] &= 0x7f;
        if (serialNumber.All(value => value == 0))
        {
            serialNumber[^1] = 1;
        }

        using X509Certificate2 issuedCertificate =
            request.Create(
                issuer,
                notBefore,
                notAfter,
                serialNumber);
        using X509Certificate2 certificate =
            issuedCertificate.CopyWithPrivateKey(key);
        using X509Certificate2 issuerPublicCertificate =
            X509CertificateLoader.LoadCertificate(
                issuer.Export(X509ContentType.Cert));
        var pfxCertificates = new X509Certificate2Collection
        {
            certificate,
            issuerPublicCertificate
        };
        File.WriteAllBytes(
            certificatePath,
            pfxCertificates.Export(X509ContentType.Pfx, password));

        return new CertificateDescriptor(
            certificatePath,
            certificate.GetCertHashString(HashAlgorithmName.SHA256));
    }

    private static string NewPassword()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(36);
        try
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    internal sealed record CertificateDescriptor(
        string CertificatePath,
        string Sha256);
}
