// SPDX-License-Identifier: MIT

using NosGM.Updater.Core;

namespace NosGM.Launcher;

internal enum InstallFolderKind
{
    Empty,
    ExistingClient,
    Managed
}

internal sealed record InstallFolderInspection(
    InstallFolderKind Kind,
    string Path,
    string? ReleaseId,
    int ManagedFiles);

internal static class InstallFolderInspector
{
    public static async Task<InstallFolderInspection> InspectAsync(
        string path,
        string gameExecutable,
        CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(path);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Installation folder does not exist: {root}");
        }

        SafePaths.EnsureNoReparsePoints(root, root);
        var state = await InstallStateStore.LoadAsync(root, cancellationToken);
        if (state.Files.Count > 0 || !string.IsNullOrWhiteSpace(state.ReleaseId))
        {
            return new InstallFolderInspection(
                InstallFolderKind.Managed,
                root,
                state.ReleaseId,
                state.Files.Count);
        }

        var gamePath = SafePaths.ResolveManagedPath(root, gameExecutable);
        if (File.Exists(gamePath))
        {
            return new InstallFolderInspection(
                InstallFolderKind.ExistingClient,
                root,
                ReleaseId: null,
                ManagedFiles: 0);
        }

        var meaningfulEntries = Directory.EnumerateFileSystemEntries(root)
            .Where(entry => !string.Equals(
                Path.GetFileName(entry),
                ".nosgm",
                StringComparison.OrdinalIgnoreCase))
            .Take(1)
            .Any();
        if (!meaningfulEntries)
        {
            return new InstallFolderInspection(
                InstallFolderKind.Empty,
                root,
                ReleaseId: null,
                ManagedFiles: 0);
        }

        throw new InvalidDataException(
            $"The selected folder is not empty and does not contain '{gameExecutable}' or a NosGM managed state.");
    }
}
