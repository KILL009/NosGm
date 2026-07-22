// SPDX-License-Identifier: GPL-3.0-only

namespace NosGM.PacketCatalog;

internal sealed class CliOptions
{
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _flags = new(StringComparer.OrdinalIgnoreCase);

    public CliOptions(string[] args)
    {
        Command = args.Length == 0 ? "help" : args[0].Trim().ToLowerInvariant();
        for (var index = 1; index < args.Length; index++)
        {
            var current = args[index];
            if (!current.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected argument: {current}");
            }

            if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                _values[current] = args[++index];
            }
            else
            {
                _flags.Add(current);
            }
        }
    }

    public string Command { get; }

    public string Root => Path.GetFullPath(Value("--root") ?? Directory.GetCurrentDirectory());

    public string OutputDirectory => Path.GetFullPath(
        Value("--output-directory") ?? Path.Combine(Root, "artifacts", "packet-catalog"));

    public string? Report => Value("--report") is { } report ? Path.GetFullPath(report) : null;

    public bool Strict => _flags.Contains("--strict");

    private string? Value(string name) => _values.TryGetValue(name, out var value) ? value : null;
}
