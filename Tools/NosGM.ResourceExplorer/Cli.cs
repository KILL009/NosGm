// SPDX-License-Identifier: BSL-1.0

namespace NosGM.ResourceExplorer;

internal sealed class Cli
{
    private readonly string[] _args;
    public Cli(string[] args) => _args = args;

    public string Command => _args.Length == 0 ? "help" : _args[0].ToLowerInvariant();

    public string Required(string name)
    {
        var value = Optional(name);
        return string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"Missing required option {name}.") : value;
    }

    public string? Optional(string name)
    {
        for (var i = 1; i < _args.Length; i++)
        {
            if (string.Equals(_args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= _args.Length || _args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException($"Option {name} requires a value.");
                return _args[i + 1];
            }
        }
        return null;
    }

    public bool Flag(string name) => _args.Skip(1).Any(value => string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
}
