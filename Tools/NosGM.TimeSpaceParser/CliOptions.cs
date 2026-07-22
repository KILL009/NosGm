// SPDX-License-Identifier: GPL-3.0-only
// Derived from Elendan/TimeSpace-Generator, the SEOVA adaptation,
// noszanou/OpennosTimeSpaceParser and the OpenNos XML model.
// Modifications Copyright (C) 2026 NosGM contributors.

using System.Globalization;

namespace NosGM.TimeSpaceParser;

internal sealed class CliOptions
{
    public string Command { get; private set; } = "help";
    public string? InputPath { get; private set; }
    public string? OutputPath { get; private set; }
    public string? InputDirectory { get; private set; }
    public string? OutputDirectory { get; private set; }
    public string Pattern { get; private set; } = "*.txt";
    public bool Strict { get; private set; }
    public bool Force { get; private set; }
    public bool Help { get; private set; }
    public string? NameOverride { get; private set; }
    public string? LabelOverride { get; private set; }
    public byte? LivesOverride { get; private set; }
    public long? GoldOverride { get; private set; }
    public int? ReputationOverride { get; private set; }

    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();
        if (args.Length == 0)
        {
            options.Help = true;
            return options;
        }

        options.Command = args[0].Trim().ToLowerInvariant();
        if (options.Command is "-h" or "--help" or "help")
        {
            options.Command = "help";
            options.Help = true;
            return options;
        }

        for (var index = 1; index < args.Length; index++)
        {
            var token = args[index];
            switch (token)
            {
                case "-h":
                case "--help":
                    options.Help = true;
                    break;
                case "--strict":
                    options.Strict = true;
                    break;
                case "--force":
                    options.Force = true;
                    break;
                case "--input":
                    options.InputPath = RequireValue(args, ref index, token);
                    break;
                case "--output":
                    options.OutputPath = RequireValue(args, ref index, token);
                    break;
                case "--input-directory":
                    options.InputDirectory = RequireValue(args, ref index, token);
                    break;
                case "--output-directory":
                    options.OutputDirectory = RequireValue(args, ref index, token);
                    break;
                case "--pattern":
                    options.Pattern = RequireValue(args, ref index, token);
                    break;
                case "--name":
                    options.NameOverride = RequireValue(args, ref index, token);
                    break;
                case "--label":
                    options.LabelOverride = RequireValue(args, ref index, token);
                    break;
                case "--lives":
                    options.LivesOverride = ParseByte(RequireValue(args, ref index, token), token);
                    break;
                case "--gold":
                    options.GoldOverride = ParseLong(RequireValue(args, ref index, token), token);
                    break;
                case "--reputation":
                    options.ReputationOverride = ParseInt(RequireValue(args, ref index, token), token);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {token}");
            }
        }

        return options;
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {option}.");
        }

        index++;
        return args[index];
    }

    private static byte ParseByte(string value, string option)
    {
        if (!byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new ArgumentException($"Invalid byte value for {option}: {value}");
        }

        return parsed;
    }

    private static int ParseInt(string value, string option)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new ArgumentException($"Invalid integer value for {option}: {value}");
        }

        return parsed;
    }

    private static long ParseLong(string value, string option)
    {
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new ArgumentException($"Invalid long value for {option}: {value}");
        }

        return parsed;
    }
}
