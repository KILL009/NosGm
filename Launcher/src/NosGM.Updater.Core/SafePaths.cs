// SPDX-License-Identifier: MIT

namespace NosGM.Updater.Core;

public static class SafePaths
{
    private static readonly HashSet<string> ReservedWindowsNames = BuildReservedWindowsNames();
    private static readonly char[] ForbiddenCharacters = ['<', '>', ':', '"', '|', '?', '*', '\\'];

    public static string NormalizeRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            !string.Equals(relativePath, relativePath.Trim(), StringComparison.Ordinal) ||
            relativePath.Length > 512 ||
            relativePath.StartsWith('/', StringComparison.Ordinal) ||
            relativePath.Contains("//", StringComparison.Ordinal) ||
            Path.IsPathRooted(relativePath) ||
            relativePath.Any(char.IsControl))
        {
            throw new InvalidDataException($"Unsafe managed path '{relativePath}'.");
        }

        var segments = relativePath.Split('/', StringSplitOptions.None);
        if (segments.Length == 0 || segments.Any(segment => !IsSafeSegment(segment)))
        {
            throw new InvalidDataException($"Unsafe managed path '{relativePath}'.");
        }

        if (string.Equals(segments[0], ".nosgm", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Release manifests cannot manage the launcher's .nosgm metadata directory.");
        }

        return string.Join('/', segments);
    }

    public static string ResolveManagedPath(string rootPath, string relativePath)
    {
        var root = Path.GetFullPath(rootPath);
        var normalized = NormalizeRelativePath(relativePath);
        var candidate = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!candidate.StartsWith(prefix, comparison))
        {
            throw new InvalidDataException($"Managed path '{relativePath}' escapes the installation root.");
        }

        EnsureNoReparsePoints(root, candidate);
        return candidate;
    }

    public static void EnsureNoReparsePoints(string rootPath, string candidatePath)
    {
        var root = Path.GetFullPath(rootPath);
        var candidate = Path.GetFullPath(candidatePath);
        InspectExistingPath(root);

        var relative = Path.GetRelativePath(root, candidate);
        var current = root;
        foreach (var segment in relative.Split(
                     Path.DirectorySeparatorChar,
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            InspectExistingPath(current);
        }
    }

    public static IEnumerable<string> EnumerateFilesWithoutReparsePoints(string rootPath)
    {
        var root = Path.GetFullPath(rootPath);
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            InspectExistingPath(directory);

            foreach (var childDirectory in Directory.EnumerateDirectories(directory))
            {
                InspectExistingPath(childDirectory);
                pending.Push(childDirectory);
            }

            foreach (var file in Directory.EnumerateFiles(directory))
            {
                InspectExistingPath(file);
                yield return file;
            }
        }
    }

    private static bool IsSafeSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment) ||
            segment is "." or ".." ||
            segment.Length > 120 ||
            segment.EndsWith(' ') ||
            segment.EndsWith('.') ||
            segment.IndexOfAny(ForbiddenCharacters) >= 0)
        {
            return false;
        }

        var stem = segment.Split('.', 2)[0];
        return !ReservedWindowsNames.Contains(stem);
    }

    private static void InspectExistingPath(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return;
        }

        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException($"Reparse points are not allowed in managed paths: '{path}'.");
        }
    }

    private static HashSet<string> BuildReservedWindowsNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL", "CLOCK$"
        };

        for (var index = 1; index <= 9; index++)
        {
            names.Add($"COM{index}");
            names.Add($"LPT{index}");
        }

        return names;
    }
}
