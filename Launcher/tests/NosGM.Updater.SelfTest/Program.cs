// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using NosGM.Updater.Core;

namespace NosGM.Updater.SelfTest;

internal static class Program
{
    public static async Task<int> Main()
    {
        var root = Path.Combine(Path.GetTempPath(), "NosGM.Updater.SelfTest", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            var installRoot = Path.Combine(root, "install");
            var releaseOneRoot = Path.Combine(root, "release-one");
            var releaseTwoRoot = Path.Combine(root, "release-two");
            var releaseThreeRoot = Path.Combine(root, "release-three");
            Directory.CreateDirectory(installRoot);

            using var signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var privateKeyPem = signingKey.ExportECPrivateKeyPem();
            var publicKeyPem = signingKey.ExportSubjectPublicKeyInfoPem();
            const string keyId = "self-test-key";

            WriteRelease(releaseOneRoot, new Dictionary<string, byte[]>
            {
                ["NostaleClientX.exe"] = BuildBytes("client-v1", 4096),
                ["data/base.dat"] = BuildBytes("base-v1", 2048)
            });

            var manifestOne = await BuildSignedManifestAsync(
                releaseOneRoot,
                "release-1",
                keyId,
                privateKeyPem,
                Array.Empty<string>());
            ManifestSecurity.Verify(manifestOne, keyId, publicKeyPem);

            var stateZero = await InstallStateStore.LoadAsync(installRoot);
            var planOne = await UpdatePlanner.CreateAsync(installRoot, manifestOne, stateZero);
            await using (var source = new DirectoryContentSource(releaseOneRoot))
            {
                var result = await new TransactionalUpdater().ApplyAsync(
                    installRoot,
                    planOne,
                    stateZero,
                    source);
                Require(result.DownloadedFiles == 2, "First release did not install two files.");
            }

            await VerifyInstalledManifestAsync(installRoot, manifestOne);
            var stateOne = await InstallStateStore.LoadAsync(installRoot);
            Require(stateOne.ReleaseId == "release-1", "First release state was not saved.");

            var unmanagedPath = Path.Combine(installRoot, "user-note.txt");
            await File.WriteAllTextAsync(unmanagedPath, "player-owned");

            WriteRelease(releaseTwoRoot, new Dictionary<string, byte[]>
            {
                ["NostaleClientX.exe"] = BuildBytes("client-v2", 4096),
                ["data/new.dat"] = BuildBytes("new-v2", 1024)
            });

            var manifestTwo = await BuildSignedManifestAsync(
                releaseTwoRoot,
                "release-2",
                keyId,
                privateKeyPem,
                ["data/base.dat", "user-note.txt"]);
            ManifestSecurity.Verify(manifestTwo, keyId, publicKeyPem);

            var planTwo = await UpdatePlanner.CreateAsync(installRoot, manifestTwo, stateOne);
            Require(planTwo.Deletes.SequenceEqual(["data/base.dat"]),
                "Managed deletion was not planned correctly.");
            Require(planTwo.IgnoredDeletes.SequenceEqual(["user-note.txt"]),
                "Unmanaged deletion was not ignored.");

            await using (var source = new DirectoryContentSource(releaseTwoRoot))
            {
                var result = await new TransactionalUpdater().ApplyAsync(
                    installRoot,
                    planTwo,
                    stateOne,
                    source);
                Require(result.IgnoredDeletes.Count == 1, "Ignored deletion was not reported.");
            }

            Require(!File.Exists(Path.Combine(installRoot, "data", "base.dat")),
                "Managed obsolete file was not removed.");
            Require(File.Exists(unmanagedPath), "Unmanaged player file was deleted.");
            await VerifyInstalledManifestAsync(installRoot, manifestTwo);

            var tamperedManifest = manifestTwo with { ReleaseId = "release-2-tampered" };
            ExpectCryptographicFailure(
                () => ManifestSecurity.Verify(tamperedManifest, keyId, publicKeyPem),
                "Tampered manifest signature was accepted.");

            var traversalManifest = manifestTwo with
            {
                Files =
                [
                    new ReleaseFile
                    {
                        Path = "../escape.exe",
                        Url = "../escape.exe",
                        Size = 1,
                        Sha256 = new string('0', 64)
                    }
                ],
                Delete = Array.Empty<string>(),
                Signature = string.Empty
            };
            ExpectInvalidData(
                () => ManifestValidator.Validate(traversalManifest, requireSignature: false),
                "Traversal path was accepted.");

            WriteRelease(releaseThreeRoot, new Dictionary<string, byte[]>
            {
                ["NostaleClientX.exe"] = BuildBytes("client-v3", 4096),
                ["data/new.dat"] = BuildBytes("new-v2", 1024)
            });
            var manifestThree = await BuildSignedManifestAsync(
                releaseThreeRoot,
                "release-3",
                keyId,
                privateKeyPem,
                Array.Empty<string>());

            var stateTwo = await InstallStateStore.LoadAsync(installRoot);
            var planThree = await UpdatePlanner.CreateAsync(installRoot, manifestThree, stateTwo);
            var installedClient = Path.Combine(installRoot, "NostaleClientX.exe");
            var beforeFailedUpdate = await Hashing.Sha256FileAsync(installedClient);
            await File.WriteAllBytesAsync(
                Path.Combine(releaseThreeRoot, "NostaleClientX.exe"),
                BuildBytes("corrupted-download", 4096));

            await using (var source = new DirectoryContentSource(releaseThreeRoot))
            {
                await ExpectAsyncFailure(
                    () => new TransactionalUpdater().ApplyAsync(
                        installRoot,
                        planThree,
                        stateTwo,
                        source),
                    "Corrupted staged download was accepted.");
            }

            var afterFailedUpdate = await Hashing.Sha256FileAsync(installedClient);
            Require(beforeFailedUpdate == afterFailedUpdate,
                "Failed staged download changed the installed client.");
            Require((await InstallStateStore.LoadAsync(installRoot)).ReleaseId == "release-2",
                "Failed update changed managed state.");

            Console.WriteLine("NosGM updater synthetic self-test passed.");
            Console.WriteLine($"Release public-key fingerprint: {ManifestSecurity.PublicKeyFingerprint(publicKeyPem)}");
            return 0;
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static async Task<ReleaseManifest> BuildSignedManifestAsync(
        string releaseRoot,
        string releaseId,
        string keyId,
        string privateKeyPem,
        IReadOnlyList<string> deletes)
    {
        var files = new List<ReleaseFile>();
        foreach (var path in SafePaths.EnumerateFilesWithoutReparsePoints(releaseRoot)
                     .OrderBy(item => item, StringComparer.Ordinal))
        {
            var relative = SafePaths.NormalizeRelativePath(
                Path.GetRelativePath(releaseRoot, path).Replace(Path.DirectorySeparatorChar, '/'));
            files.Add(new ReleaseFile
            {
                Path = relative,
                Url = relative,
                Size = new FileInfo(path).Length,
                Sha256 = await Hashing.Sha256FileAsync(path)
            });
        }

        var unsigned = new ReleaseManifest
        {
            ReleaseId = releaseId,
            ClientVersion = "0.9.3.3255",
            MinimumLauncherVersion = "1.0.0",
            KeyId = keyId,
            Files = files,
            Delete = deletes,
            Signature = string.Empty
        };
        return unsigned with { Signature = ManifestSecurity.Sign(unsigned, privateKeyPem) };
    }

    private static void WriteRelease(string root, IReadOnlyDictionary<string, byte[]> files)
    {
        Directory.CreateDirectory(root);
        foreach (var pair in files)
        {
            var path = SafePaths.ResolveManagedPath(root, pair.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, pair.Value);
        }
    }

    private static byte[] BuildBytes(string seed, int size)
    {
        var seedBytes = System.Text.Encoding.UTF8.GetBytes(seed);
        return Enumerable.Range(0, size)
            .Select(index => seedBytes[index % seedBytes.Length])
            .ToArray();
    }

    private static async Task VerifyInstalledManifestAsync(string installRoot, ReleaseManifest manifest)
    {
        foreach (var file in manifest.Files)
        {
            var path = SafePaths.ResolveManagedPath(installRoot, file.Path);
            Require(File.Exists(path), $"Installed file '{file.Path}' is missing.");
            Require(new FileInfo(path).Length == file.Size, $"Installed size differs for '{file.Path}'.");
            Require(
                string.Equals(await Hashing.Sha256FileAsync(path), file.Sha256, StringComparison.OrdinalIgnoreCase),
                $"Installed hash differs for '{file.Path}'.");
        }
    }

    private static void ExpectCryptographicFailure(Action action, string message)
    {
        try
        {
            action();
        }
        catch (CryptographicException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void ExpectInvalidData(Action action, string message)
    {
        try
        {
            action();
        }
        catch (InvalidDataException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static async Task ExpectAsyncFailure(Func<Task> action, string message)
    {
        try
        {
            await action();
        }
        catch (InvalidDataException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
