using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using WireNodeRole = NosGm.Cluster.Wire.V1.ClusterNodeRole;

namespace NosGm.Authentication.Server.Security;

public sealed class ClientCertificateRoleMap
{
    private readonly IReadOnlyDictionary<string, WireNodeRole> _roles;

    public ClientCertificateRoleMap(AuthenticationServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var roles = new Dictionary<string, WireNodeRole>(
            StringComparer.Ordinal);
        foreach (KeyValuePair<
                     WireNodeRole,
                     IReadOnlyCollection<string>> role
                 in options.AllowedFingerprints)
        {
            foreach (string fingerprint in role.Value)
            {
                roles.Add(fingerprint, role.Key);
            }
        }

        _roles = roles;
    }

    public bool IsKnownCertificate(X509Certificate2 certificate)
    {
        return TryResolveRole(certificate, out _);
    }

    public bool TryResolveRole(
        X509Certificate2 certificate,
        out WireNodeRole role)
    {
        role = WireNodeRole.Unspecified;
        if (certificate == null)
        {
            return false;
        }

        string fingerprint =
            certificate.GetCertHashString(HashAlgorithmName.SHA256);
        return TryResolveFingerprint(fingerprint, out role);
    }

    public bool TryResolveFingerprint(
        string fingerprint,
        out WireNodeRole role)
    {
        role = WireNodeRole.Unspecified;
        return !string.IsNullOrEmpty(fingerprint) &&
               _roles.TryGetValue(
                   fingerprint.ToUpperInvariant(),
                   out role);
    }
}
