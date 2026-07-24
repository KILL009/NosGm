// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using NosGM.Updater.Core;

namespace NosGM.Updater.SelfTest;

internal static class PhaseTwoSelfTest
{
    public static async Task RunAsync(
        string root,
        string keyId,
        string privateKeyPem,
        string publicKeyPem)
    {
        await TestExplicitImportAsync(root, keyId, privateKeyPem, publicKeyPem);
        await TestInterruptedCommitRecoveryAsync(root);
        await TestCommittedTransactionFinalizationAsync(root);
    }

    private static async Task TestExplicitImportAsync(
        string root,
        string keyId,
        string privateKeyPem,
        string publicKeyPem)
    {
        var installRoot = Path.Combine(root, "phase-two-import");
        Directory.CreateDirectory(Path.Combine(installRoot, "data"));
        var clientBytes = BuildBytes("existing-client", 2048);
        var desiredData = BuildBytes("desired-data", 1024);
        await File.WriteAllBytesAsync(Path.Combine(installRoot, "NostaleClientX.exe"), clientBytes);
        await File.WriteAllBytesAsync(Path.Combine(installRoot, "data", "base.dat"), BuildBytes("old-data", 1024));
        await File.WriteAllTextAsync(Path.Combine(installRoot, "player-note.txt"), "untouched");

        var unsigned = new ReleaseManifest
        {
            ReleaseId = "import-target",
            ClientVersion = "0.9.3.3255",
            MinimumLauncherVersion = "1.0.0",
            KeyId = keyId,
            Files =
            [
                CreateFile("NostaleClientX.exe", clientBytes),
                CreateFile("data/base.dat", desiredData),
                CreateFile("data/missing.dat", BuildBytes("missing", 512))
            ],
            Signature = string.Empty
        };
        var manifest = unsigned with { Signature = ManifestSecurity.Sign(unsigned, privateKeyPem) };
        var verified = ManifestSecurity.Verify(manifest, keyId, publicKeyPem);
        var result = await ExistingInstallImporter.AdoptAsync(installRoot, verified);

        Require(result.ManagedFiles == 2, "Explicit import did not manage the two existing signed paths.");
        Require(result.MatchingFiles == 1, "Explicit import matching count is incorrect.");
        Require(result.RepairFiles == 1, "Explicit import repair count is incorrect.");
        Require(result.MissingFiles == 1, "Explicit import missing count is incorrect.");
        Require(await File.ReadAllTextAsync(Path.Combine(installRoot, "player-note.txt")) == "untouched",
            "Explicit import changed an unrelated player file.");

        var state = await InstallStateStore.LoadAsync(installRoot);
        Require(state.Files.Count == 2 && !state.Files.ContainsKey("player-note.txt"),
            "Explicit import adopted a path outside the signed manifest.");
        var plan = await UpdatePlanner.CreateAsync(installRoot, verified);
        Require(plan.Downloads.Select(file => file.Path).OrderBy(path => path, StringComparer.Ordinal)
                .SequenceEqual(new[] { "data/base.dat", "data/missing.dat" }),
            "Imported installation did not produce the expected repair plan.");
    }

    private static async Task TestInterruptedCommitRecoveryAsync(string root)
    {
        var installRoot = Path.Combine(root, "phase-two-recovery");
        Directory.CreateDirectory(installRoot);
        var oldBytes = BuildBytes("old-managed", 512);
        var newBytes = BuildBytes("new-managed", 512);
        var destination = Path.Combine(installRoot, "managed.bin");
        await File.WriteAllBytesAsync(destination, oldBytes);
        var oldState = State("old-release", oldBytes);
        await InstallStateStore.SaveAsync(installRoot, oldState);

        var transactionId = Guid.NewGuid().ToString("N");
        var transactionRoot = Path.Combine(
            InstallStateStore.GetMetadataRoot(installRoot),
            "transactions",
            transactionId);
        var rollbackFile = Path.Combine(transactionRoot, "rollback", "managed.bin");
        var stateBackup = Path.Combine(transactionRoot, "rollback", "__state", "state.json");
        Directory.CreateDirectory(Path.GetDirectoryName(rollbackFile)!);
        Directory.CreateDirectory(Path.GetDirectoryName(stateBackup)!);
        File.Move(destination, rollbackFile);
        await File.WriteAllBytesAsync(destination, newBytes);
        File.Copy(InstallStateStore.GetStatePath(installRoot), stateBackup);

        var targetState = State("new-release", newBytes);
        await JsonSupport.WriteAtomicAsync(Path.Combine(transactionRoot, "journal.json"), new
        {
            schemaVersion = 1,
            transactionId,
            phase = "committing",
            stateExisted = true,
            targetState,
            operations = new[]
            {
                new { path = "managed.bin", kind = "replace", hadOriginal = true }
            }
        });

        var result = await TransactionRecovery.RecoverAsync(installRoot);
        Require(result.RecoveredTransactions == 1, "Interrupted transaction was not recovered.");
        Require((await File.ReadAllBytesAsync(destination)).SequenceEqual(oldBytes),
            "Recovery did not restore the original managed file.");
        Require((await InstallStateStore.LoadAsync(installRoot)).ReleaseId == "old-release",
            "Recovery did not restore the previous managed state.");
        Require(!Directory.Exists(transactionRoot), "Recovered transaction directory was not removed.");
    }

    private static async Task TestCommittedTransactionFinalizationAsync(string root)
    {
        var installRoot = Path.Combine(root, "phase-two-finalize");
        Directory.CreateDirectory(installRoot);
        var oldBytes = BuildBytes("finalize-old", 512);
        var newBytes = BuildBytes("finalize-new", 512);
        var destination = Path.Combine(installRoot, "managed.bin");
        await File.WriteAllBytesAsync(destination, newBytes);
        var targetState = State("final-release", newBytes);
        await InstallStateStore.SaveAsync(installRoot, targetState);

        var transactionId = Guid.NewGuid().ToString("N");
        var transactionRoot = Path.Combine(
            InstallStateStore.GetMetadataRoot(installRoot),
            "transactions",
            transactionId);
        var rollbackFile = Path.Combine(transactionRoot, "rollback", "managed.bin");
        Directory.CreateDirectory(Path.GetDirectoryName(rollbackFile)!);
        await File.WriteAllBytesAsync(rollbackFile, oldBytes);
        await JsonSupport.WriteAtomicAsync(Path.Combine(transactionRoot, "journal.json"), new
        {
            schemaVersion = 1,
            transactionId,
            phase = "committing",
            stateExisted = true,
            targetState,
            operations = new[]
            {
                new { path = "managed.bin", kind = "replace", hadOriginal = true }
            }
        });

        var result = await TransactionRecovery.RecoverAsync(installRoot);
        Require(result.FinalizedTransactions == 1, "Committed transaction was not finalized.");
        Require((await File.ReadAllBytesAsync(destination)).SequenceEqual(newBytes),
            "Finalization rolled back an already committed release.");
        Require(!Directory.Exists(transactionRoot), "Finalized transaction directory was not removed.");
    }

    private static ReleaseFile CreateFile(string path, byte[] bytes)
        => new()
        {
            Path = path,
            Url = path,
            Size = bytes.LongLength,
            Sha256 = Convert.ToHexString(SHA256.HashData(bytes))
        };

    private static ManagedInstallState State(string releaseId, byte[] bytes)
        => new()
        {
            ReleaseId = releaseId,
            ClientVersion = "0.9.3.3255",
            Files = new Dictionary<string, ManagedFileState>(StringComparer.OrdinalIgnoreCase)
            {
                ["managed.bin"] = new ManagedFileState
                {
                    Size = bytes.LongLength,
                    Sha256 = Convert.ToHexString(SHA256.HashData(bytes))
                }
            }
        };

    private static byte[] BuildBytes(string seed, int size)
    {
        var seedBytes = System.Text.Encoding.UTF8.GetBytes(seed);
        return Enumerable.Range(0, size)
            .Select(index => seedBytes[index % seedBytes.Length])
            .ToArray();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
