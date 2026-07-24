// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NosGM.Updater.Core;

namespace NosGM.ManifestBuilder;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var command = Cli.Parse(args);
            return command.Name switch
            {
                "keygen" => GenerateKeys(command),
                "build" => await BuildManifestAsync(command),
                "verify" => await VerifyManifestAsync(command),
                "channel" => GenerateTrustedChannel(command),
                "fingerprint" => PrintFingerprint(command),
                "help" => Help(),
                _ => throw new ArgumentException($"Unknown command '{command.Name}'.")
            };
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or InvalidDataException or
            CryptographicException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            return 1;
        }
    }

    private static int GenerateKeys(Cli command)
    {
        var privatePath = EnsureNewOutput(command.Required("private-key"));
        var publicPath = EnsureNewOutput(command.Required("public-key"));
        EnsureDistinct(privatePath, publicPath, "Private and public key paths must differ.");

        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        Directory.CreateDirectory(Path.GetDirectoryName(privatePath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(publicPath)!);
        File.WriteAllText(privatePath, key.ExportECPrivateKeyPem());
        File.WriteAllText(publicPath, key.ExportSubjectPublicKeyInfoPem());

        Console.WriteLine($"Private key written: {privatePath}");
        Console.WriteLine($"Public key written:  {publicPath}");
        Console.WriteLine($"Public fingerprint: {ManifestSecurity.PublicKeyFingerprint(File.ReadAllText(publicPath))}");
        Console.WriteLine("Keep the private key offline and outside every repository, CDN and game server.");
        return 0;
    }

    private static async Task<int> BuildManifestAsync(Cli command)
    {
        var root = Path.GetFullPath(command.Required("root"));
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Release root '{root}' does not exist.");
        }

        var output = Path.GetFullPath(command.Required("output"));
        var privateKeyPath = Path.GetFullPath(command.Required("private-key"));
        EnsureDistinct(output, privateKeyPath, "Manifest output cannot replace the private key.");
        if (File.Exists(output))
        {
            throw new IOException($"Manifest output '{output}' already exists.");
        }

        var files = new List<ReleaseFile>();
        foreach (var filePath in SafePaths.EnumerateFilesWithoutReparsePoints(root)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            if (PathsEqual(filePath, output))
            {
                continue;
            }

            var relative = Path.GetRelativePath(root, filePath)
                .Replace(Path.DirectorySeparatorChar, '/');
            relative = SafePaths.NormalizeRelativePath(relative);
            var info = new FileInfo(filePath);
            var hash = await Hashing.Sha256FileAsync(filePath);
            files.Add(new ReleaseFile
            {
                Path = relative,
                Url = relative,
                Size = info.Length,
                Sha256 = hash
            });
        }

        if (files.Count == 0)
        {
            throw new InvalidDataException("Release root contains no publishable files.");
        }

        var unsigned = new ReleaseManifest
        {
            ReleaseId = command.Required("release-id"),
            ClientVersion = command.Required("client-version"),
            MinimumLauncherVersion = command.Required("minimum-launcher-version"),
            KeyId = command.Required("key-id"),
            Files = files,
            Delete = ReadDeleteList(command.Optional("delete-list")),
            Signature = string.Empty
        };

        var privateKeyPem = File.ReadAllText(privateKeyPath);
        var signed = unsigned with { Signature = ManifestSecurity.Sign(unsigned, privateKeyPem) };
        await ManifestIO.WriteAsync(output, signed);
        Console.WriteLine($"Signed manifest written: {output}");
        Console.WriteLine($"Release files: {signed.Files.Count}");
        Console.WriteLine($"Release bytes: {signed.Files.Sum(file => file.Size)}");
        return 0;
    }

    private static async Task<int> VerifyManifestAsync(Cli command)
    {
        var manifest = await ManifestIO.ReadFileAsync(command.Required("manifest"));
        var publicKeyPem = ReadPublicP256Key(command.Required("public-key"));
        ManifestSecurity.Verify(manifest, command.Required("key-id"), publicKeyPem);
        Console.WriteLine($"Manifest '{manifest.ReleaseId}' is valid.");
        Console.WriteLine($"Public fingerprint: {ManifestSecurity.PublicKeyFingerprint(publicKeyPem)}");
        return 0;
    }

    private static int GenerateTrustedChannel(Cli command)
    {
        var output = EnsureNewOutput(command.Required("output"));
        var publicKeyPath = Path.GetFullPath(command.Required("public-key"));
        EnsureDistinct(output, publicKeyPath, "Generated channel source cannot replace the public key.");

        var manifestUri = ParseCleanHttpsUri(command.Required("manifest-uri"), requireTrailingSlash: false);
        var contentBaseUri = ParseCleanHttpsUri(command.Required("content-base-uri"), requireTrailingSlash: true);
        var keyId = ValidateKeyId(command.Required("key-id"));
        var publicKeyPem = ReadPublicP256Key(publicKeyPath);
        var publicKeyBase64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(publicKeyPem));

        var lines = new[]
        {
            "// <auto-generated />",
            "// SPDX-License-Identifier: MIT",
            "",
            "namespace NosGM.Launcher;",
            "",
            "internal static class TrustedChannelConfiguration",
            "{",
            $"    public const string ManifestUriText = {CSharpLiteral(manifestUri.AbsoluteUri)};",
            $"    public const string ContentBaseUriText = {CSharpLiteral(contentBaseUri.AbsoluteUri)};",
            $"    public const string KeyId = {CSharpLiteral(keyId)};",
            $"    private const string PublicKeyBase64 = {CSharpLiteral(publicKeyBase64)};",
            "    public static string PublicKeyPem { get; } =",
            "        System.Text.Encoding.ASCII.GetString(System.Convert.FromBase64String(PublicKeyBase64));",
            "}",
            ""
        };

        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        File.WriteAllText(
            output,
            string.Join(Environment.NewLine, lines),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        Console.WriteLine($"Trusted channel source written: {output}");
        Console.WriteLine($"Public fingerprint: {ManifestSecurity.PublicKeyFingerprint(publicKeyPem)}");
        return 0;
    }

    private static int PrintFingerprint(Cli command)
    {
        var publicKeyPem = ReadPublicP256Key(command.Required("public-key"));
        Console.WriteLine(ManifestSecurity.PublicKeyFingerprint(publicKeyPem));
        return 0;
    }

    private static string ReadPublicP256Key(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var text = File.ReadAllText(fullPath);
        if (text.Contains("PRIVATE KEY", StringComparison.Ordinal))
        {
            throw new CryptographicException("A public-key file is required; private key material was supplied.");
        }

        using var key = ECDsa.Create();
        key.ImportFromPem(text);
        if (key.KeySize != 256)
        {
            throw new CryptographicException($"Release public key must use ECDSA P-256; imported size is {key.KeySize} bits.");
        }

        return key.ExportSubjectPublicKeyInfoPem();
    }

    private static Uri ParseCleanHttpsUri(string value, bool requireTrailingSlash)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            uri.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase) ||
            uri.AbsolutePath.Contains("//", StringComparison.Ordinal) ||
            (requireTrailingSlash && !uri.AbsolutePath.EndsWith("/", StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                requireTrailingSlash
                    ? "Content base URI must be a clean absolute HTTPS URI ending with '/'."
                    : "Manifest URI must be a clean absolute HTTPS URI.");
        }

        return uri;
    }

    private static string ValidateKeyId(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > 64 ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
            string.Equals(value, "UNCONFIGURED", StringComparison.Ordinal) ||
            value.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('.' or '_' or '-')))
        {
            throw new ArgumentException("Key id must contain 1-64 ASCII letters, digits, '.', '_' or '-'.");
        }

        return value;
    }

    private static string CSharpLiteral(string value)
        => JsonSerializer.Serialize(value);

    private static IReadOnlyList<string> ReadDeleteList(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Array.Empty<string>();
        }

        var fullPath = Path.GetFullPath(path);
        var info = new FileInfo(fullPath);
        if (!info.Exists || info.Length > JsonSupport.MaxJsonBytes)
        {
            throw new InvalidDataException("Delete-list file is missing or too large.");
        }

        return File.ReadAllLines(fullPath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(SafePaths.NormalizeRelativePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();
    }

    private static string EnsureNewOutput(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath) || Directory.Exists(fullPath))
        {
            throw new IOException($"Output '{fullPath}' already exists.");
        }

        return fullPath;
    }

    private static void EnsureDistinct(string left, string right, string message)
    {
        if (PathsEqual(left, right))
        {
            throw new ArgumentException(message);
        }
    }

    private static bool PathsEqual(string left, string right)
        => string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static int Help()
    {
        Console.WriteLine("""
NosGM.ManifestBuilder

keygen --private-key <private.pem> --public-key <public.pem>
build --root <release-dir> --release-id <id> --client-version <version> --minimum-launcher-version <version> --key-id <id> --private-key <private.pem> --output <manifest.json> [--delete-list <paths.txt>]
verify --manifest <manifest.json> --public-key <public.pem> --key-id <id>
channel --manifest-uri <https-url> --content-base-uri <https-url/> --key-id <id> --public-key <public.pem> --output <TrustedChannel.Generated.cs>
fingerprint --public-key <public.pem>
""");
        return 0;
    }

    private sealed class Cli
    {
        private readonly Dictionary<string, string?> _options;

        private Cli(string name, Dictionary<string, string?> options)
        {
            Name = name;
            _options = options;
        }

        public string Name { get; }

        public static Cli Parse(string[] args)
        {
            if (args.Length == 0)
            {
                return new Cli("help", new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase));
            }

            var options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var index = 1; index < args.Length; index++)
            {
                var token = args[index];
                if (!token.StartsWith("--", StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Unexpected argument '{token}'.");
                }

                if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Option '{token}' requires a value.");
                }

                if (!options.TryAdd(token[2..], args[++index]))
                {
                    throw new ArgumentException($"Duplicate option '{token}'.");
                }
            }

            return new Cli(args[0].ToLowerInvariant(), options);
        }

        public string Required(string name)
            => _options.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException($"Missing required option '--{name}'.");

        public string? Optional(string name)
            => _options.TryGetValue(name, out var value) ? value : null;
    }
}
