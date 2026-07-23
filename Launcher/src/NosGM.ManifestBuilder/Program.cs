// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
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
                "help" => Help(),
                _ => throw new ArgumentException($"Unknown command '{command.Name}'.")
            };
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or InvalidDataException or CryptographicException or UnauthorizedAccessException)
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
        var publicKeyPem = File.ReadAllText(command.Required("public-key"));
        ManifestSecurity.Verify(manifest, command.Required("key-id"), publicKeyPem);
        Console.WriteLine($"Manifest '{manifest.ReleaseId}' is valid.");
        Console.WriteLine($"Public fingerprint: {ManifestSecurity.PublicKeyFingerprint(publicKeyPem)}");
        return 0;
    }

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
