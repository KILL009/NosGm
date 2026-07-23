// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;

namespace NosGM.Updater.Core;

public static class ManifestSecurity
{
    public static byte[] CreateCanonicalPayload(ReleaseManifest manifest)
    {
        using var memory = new MemoryStream();
        using (var writer = new Utf8JsonWriter(memory, new JsonWriterOptions
               {
                   Indented = false,
                   SkipValidation = false
               }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", manifest.SchemaVersion);
            writer.WriteString("releaseId", manifest.ReleaseId);
            writer.WriteString("clientVersion", manifest.ClientVersion);
            writer.WriteString("minimumLauncherVersion", manifest.MinimumLauncherVersion);
            writer.WriteString("signatureAlgorithm", manifest.SignatureAlgorithm);
            writer.WriteString("keyId", manifest.KeyId);

            writer.WritePropertyName("files");
            writer.WriteStartArray();
            foreach (var file in manifest.Files
                         .OrderBy(item => item.Path, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("path", file.Path);
                writer.WriteNumber("size", file.Size);
                writer.WriteString("sha256", file.Sha256.ToUpperInvariant());
                writer.WriteString("url", file.Url);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WritePropertyName("delete");
            writer.WriteStartArray();
            foreach (var path in manifest.Delete.OrderBy(item => item, StringComparer.Ordinal))
            {
                writer.WriteStringValue(path);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return memory.ToArray();
    }

    public static string Sign(ReleaseManifest unsignedManifest, string privateKeyPem)
    {
        ManifestValidator.Validate(unsignedManifest, requireSignature: false);
        using var key = ECDsa.Create();
        key.ImportFromPem(privateKeyPem);
        EnsureP256(key);
        var signature = key.SignData(
            CreateCanonicalPayload(unsignedManifest),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);
        return Convert.ToBase64String(signature);
    }

    public static VerifiedReleaseManifest Verify(
        ReleaseManifest manifest,
        string expectedKeyId,
        string publicKeyPem)
    {
        ManifestValidator.Validate(manifest, requireSignature: true);
        if (!string.Equals(manifest.KeyId, expectedKeyId, StringComparison.Ordinal))
        {
            throw new CryptographicException(
                $"Manifest key id '{manifest.KeyId}' does not match trusted key id '{expectedKeyId}'.");
        }

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(manifest.Signature);
        }
        catch (FormatException exception)
        {
            throw new CryptographicException("Manifest signature is not valid Base64.", exception);
        }

        using var key = ECDsa.Create();
        key.ImportFromPem(publicKeyPem);
        EnsureP256(key);
        var valid = key.VerifyData(
            CreateCanonicalPayload(manifest),
            signature,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);

        if (!valid)
        {
            throw new CryptographicException("Manifest signature verification failed.");
        }

        return new VerifiedReleaseManifest(manifest, PublicKeyFingerprint(publicKeyPem));
    }

    public static string PublicKeyFingerprint(string publicKeyPem)
    {
        using var key = ECDsa.Create();
        key.ImportFromPem(publicKeyPem);
        EnsureP256(key);
        var subjectPublicKeyInfo = key.ExportSubjectPublicKeyInfo();
        return Convert.ToHexString(SHA256.HashData(subjectPublicKeyInfo));
    }

    private static void EnsureP256(ECDsa key)
    {
        if (key.KeySize != 256)
        {
            throw new CryptographicException(
                $"Release key must use ECDSA P-256; imported key size is {key.KeySize} bits.");
        }
    }
}
