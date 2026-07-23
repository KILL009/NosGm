// SPDX-License-Identifier: MIT

namespace NosGM.Updater.Core;

public static class InstallStateStore
{
    public static string GetMetadataRoot(string installRoot)
        => Path.Combine(Path.GetFullPath(installRoot), ".nosgm");

    public static string GetStatePath(string installRoot)
        => Path.Combine(GetMetadataRoot(installRoot), "state.json");

    public static async Task<ManagedInstallState> LoadAsync(
        string installRoot,
        CancellationToken cancellationToken = default)
    {
        var statePath = GetStatePath(installRoot);
        if (!File.Exists(statePath))
        {
            return new ManagedInstallState();
        }

        var state = await JsonSupport.ReadAsync<ManagedInstallState>(statePath, cancellationToken);
        Validate(state);
        return state;
    }

    public static async Task SaveAsync(
        string installRoot,
        ManagedInstallState state,
        CancellationToken cancellationToken = default)
    {
        Validate(state);
        Directory.CreateDirectory(GetMetadataRoot(installRoot));
        await JsonSupport.WriteAtomicAsync(GetStatePath(installRoot), state, cancellationToken);
    }

    public static ManagedInstallState FromManifest(ReleaseManifest manifest)
    {
        var files = manifest.Files.ToDictionary(
            file => file.Path,
            file => new ManagedFileState
            {
                Size = file.Size,
                Sha256 = file.Sha256.ToUpperInvariant()
            },
            StringComparer.OrdinalIgnoreCase);

        return new ManagedInstallState
        {
            ReleaseId = manifest.ReleaseId,
            ClientVersion = manifest.ClientVersion,
            Files = files
        };
    }

    public static void Validate(ManagedInstallState state)
    {
        if (state.SchemaVersion != 1 || state.Files is null)
        {
            throw new InvalidDataException("Managed install state is unsupported or incomplete.");
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in state.Files)
        {
            var normalized = SafePaths.NormalizeRelativePath(pair.Key);
            if (!string.Equals(normalized, pair.Key, StringComparison.Ordinal) ||
                !seen.Add(normalized) ||
                pair.Value is null ||
                pair.Value.Size < 0 ||
                !Hashing.IsSha256(pair.Value.Sha256))
            {
                throw new InvalidDataException($"Managed install state contains an invalid entry '{pair.Key}'.");
            }
        }
    }
}
