// SPDX-License-Identifier: MIT

namespace NosGM.ClientThemeEditor;

internal sealed record ClientIdentity(
    string FileName,
    string Architecture,
    string FileVersion,
    long Length,
    string Sha256);

internal sealed record ThemeProfile
{
    public int SchemaVersion { get; init; } = 1;
    public string ProfileName { get; init; } = string.Empty;
    public bool ResearchOnly { get; init; }
    public string ExpectedFileName { get; init; } = string.Empty;
    public string ExpectedArchitecture { get; init; } = "x86";
    public string ExpectedFileVersion { get; init; } = string.Empty;
    public long ExpectedLength { get; init; }
    public string ExpectedSha256 { get; init; } = string.Empty;
    public IReadOnlyList<PatchDefinition> Patches { get; init; } = Array.Empty<PatchDefinition>();
}

internal sealed record PatchDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public string PatternHex { get; init; } = string.Empty;
    public int ExpectedMatches { get; init; } = 1;
    public int ValueOffset { get; init; }
    public string ExpectedOriginalHex { get; init; } = string.Empty;
    public string ColorEncoding { get; init; } = "RGBA";
}

internal sealed record ThemeDocument
{
    public int SchemaVersion { get; init; } = 1;
    public string ThemeName { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Colors { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

internal sealed record PlannedPatch(
    string Id,
    string Description,
    int MatchOffset,
    int ValueOffset,
    string OriginalHex,
    string ReplacementHex);

internal sealed record PatchPlan(
    ClientIdentity Identity,
    string ProfileName,
    string ThemeName,
    IReadOnlyList<PlannedPatch> Operations);

internal sealed record PatchManifest
{
    public int SchemaVersion { get; init; } = 1;
    public string ProfileName { get; init; } = string.Empty;
    public string ThemeName { get; init; } = string.Empty;
    public string OriginalPath { get; init; } = string.Empty;
    public string PatchedPath { get; init; } = string.Empty;
    public string? BackupPath { get; init; }
    public string OriginalSha256 { get; init; } = string.Empty;
    public string PatchedSha256 { get; init; } = string.Empty;
    public IReadOnlyList<PlannedPatch> Operations { get; init; } = Array.Empty<PlannedPatch>();
}

internal sealed class CliOptions
{
    private readonly Dictionary<string, string?> _values;

    private CliOptions(string command, Dictionary<string, string?> values)
    {
        Command = command;
        _values = values;
    }

    public string Command { get; }

    public static CliOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new CliOptions("help", new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase));
        }

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; index < args.Length; index++)
        {
            var token = args[index];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected argument '{token}'.");
            }

            var key = token[2..];
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Option name cannot be empty.");
            }

            string? value = null;
            if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                value = args[++index];
            }

            if (!values.TryAdd(key, value))
            {
                throw new ArgumentException($"Duplicate option '--{key}'.");
            }
        }

        return new CliOptions(args[0].ToLowerInvariant(), values);
    }

    public void EnsureOnly(params string[] allowed)
    {
        var unknown = _values.Keys
            .Where(key => !allowed.Contains(key, StringComparer.OrdinalIgnoreCase))
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unknown.Length > 0)
        {
            throw new ArgumentException($"Unknown option(s): {string.Join(", ", unknown.Select(key => $"--{key}"))}.");
        }
    }

    public string Required(string name)
        => _values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Missing required option '--{name}'.");

    public string? Optional(string name)
    {
        if (!_values.TryGetValue(name, out var value))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Option '--{name}' requires a value.");
        }

        return value;
    }

    public bool Flag(string name)
    {
        if (!_values.TryGetValue(name, out var value))
        {
            return false;
        }

        if (value is not null)
        {
            throw new ArgumentException($"Flag '--{name}' does not accept a value.");
        }

        return true;
    }
}
