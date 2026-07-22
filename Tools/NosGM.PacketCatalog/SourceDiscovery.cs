// SPDX-License-Identifier: GPL-3.0-only

namespace NosGM.PacketCatalog;

internal static class SourceDiscovery
{
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", "bin", "obj", "packages", "node_modules", "artifacts"
    };

    public static IReadOnlyList<string> FindCSharpFiles(string root)
    {
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Repository root does not exist: {root}");
        }

        var files = new List<string>();
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var child in Directory.EnumerateDirectories(directory).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (!ExcludedDirectories.Contains(Path.GetFileName(child)))
                {
                    pending.Push(child);
                }
            }

            files.AddRange(Directory.EnumerateFiles(directory, "*.cs", SearchOption.TopDirectoryOnly));
        }

        return files
            .Select(Path.GetFullPath)
            .OrderBy(path => NormalizeRelative(root, path), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string NormalizeRelative(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');
}
