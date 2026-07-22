// SPDX-License-Identifier: BSL-1.0

namespace NosGM.ResourceExplorer;

internal static class ExtractionSandbox
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static string GetSafePath(string rootDirectory, ArchiveEntry entry)
    {
        var root = Path.GetFullPath(rootDirectory);
        Directory.CreateDirectory(root);
        var fileName = Sanitize(entry.Name);
        if (string.IsNullOrWhiteSpace(Path.GetExtension(fileName)) && entry.EncodingHint is not null)
        {
            fileName += ".bin";
        }
        var candidate = Path.GetFullPath(Path.Combine(root, $"{entry.Index:D6}_{fileName}"));
        var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The calculated extraction path escaped the output directory.");
        }
        return candidate;
    }

    private static string Sanitize(string name)
    {
        var leaf = Path.GetFileName(name.Replace('\\', '/'));
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(leaf.Select(c => invalid.Contains(c) || c is '/' or '\\' ? '_' : c).ToArray()).Trim().TrimEnd('.');
        if (safe is "" or "." or "..") safe = "entry";
        if (Reserved.Contains(Path.GetFileNameWithoutExtension(safe))) safe = "_" + safe;
        return safe.Length > 180 ? safe[..180] : safe;
    }
}
