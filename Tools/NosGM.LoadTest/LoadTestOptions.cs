using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace NosGM.LoadTest;

internal enum LoadScenario
{
    Tcp,
    Login,
    World
}

internal sealed class LoadTestOptions
{
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 1337;
    public string LoginHost { get; init; } = "127.0.0.1";
    public int LoginPort { get; init; } = 4000;
    public LoadScenario Scenario { get; init; } = LoadScenario.Tcp;
    public int[] Stages { get; init; } = [100, 250, 500, 750, 1000, 1250, 1500];
    public int RampPerSecond { get; init; } = 100;
    public int HoldSeconds { get; init; } = 30;
    public int ConnectTimeoutMilliseconds { get; init; } = 5000;
    public int ReadTimeoutMilliseconds { get; init; } = 5000;
    public string? AccountsPath { get; init; }
    public byte Region { get; init; } = 5;
    public string ClientVersion { get; init; } = "0.9.3.3254";
    public string GameforgeId { get; init; } = "0";
    public string ClientDataOld { get; init; } = "0.9.3.3254";
    public string ClientData { get; init; } = "0.9.3.3254";
    public string LoginPacketTemplate { get; init; } =
        "NoS0575 0 {username} {password} {gameforgeId} {clientDataOld} {region} {clientData}";
    public string WorldReadyPacket { get; init; } = "finit";
    public string OutputDirectory { get; init; } = Path.Combine(
        "artifacts",
        "load-test",
        DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
    public string[] ProcessNames { get; init; } =
    [
        "NosGm.World",
        "NosGm.Login",
        "NosGm.Master.Server",
        "NosGm.Authentication.Server"
    ];
    public bool AllowPublicTarget { get; init; }
    public bool SelfTest { get; init; }
    public bool ShowHelp { get; init; }

    public static LoadTestOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < args.Length; index++)
        {
            string token = args[index];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected argument '{token}'. Options must start with --.");
            }

            string key = token[2..];
            if (key is "self-test" or "allow-public-target" or "help" or "h")
            {
                flags.Add(key);
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for --{key}.");
            }

            values[key] = args[++index];
        }

        LoadScenario scenario = ParseScenario(Get(values, "scenario", "tcp"));
        string host = Get(values, "host", "127.0.0.1");
        int[] stages = ParseStages(Get(values, "stages", "100,250,500,750,1000,1250,1500"));
        string processNames = Get(
            values,
            "process-names",
            "NosGm.World,NosGm.Login,NosGm.Master.Server,NosGm.Authentication.Server");

        var options = new LoadTestOptions
        {
            Host = host,
            Port = ParseInt(values, "port", scenario == LoadScenario.Login ? 4000 : 1337, 1, 65535),
            LoginHost = Get(values, "login-host", host),
            LoginPort = ParseInt(values, "login-port", 4000, 1, 65535),
            Scenario = scenario,
            Stages = stages,
            RampPerSecond = ParseInt(values, "ramp-per-second", 100, 1, 5000),
            HoldSeconds = ParseInt(values, "hold-seconds", 30, 0, 3600),
            ConnectTimeoutMilliseconds = ParseInt(values, "connect-timeout-ms", 5000, 100, 120000),
            ReadTimeoutMilliseconds = ParseInt(values, "read-timeout-ms", 5000, 100, 120000),
            AccountsPath = GetOptional(values, "accounts"),
            Region = checked((byte)ParseInt(values, "region", 5, 0, 255)),
            ClientVersion = Get(values, "client-version", "0.9.3.3254"),
            GameforgeId = Get(values, "gameforge-id", "0"),
            ClientDataOld = Get(values, "client-data-old", "0.9.3.3254"),
            ClientData = Get(values, "client-data", "0.9.3.3254"),
            LoginPacketTemplate = Get(
                values,
                "login-template",
                "NoS0575 0 {username} {password} {gameforgeId} {clientDataOld} {region} {clientData}"),
            WorldReadyPacket = Get(values, "world-ready-packet", "finit"),
            OutputDirectory = Get(
                values,
                "output",
                Path.Combine(
                    "artifacts",
                    "load-test",
                    DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture))),
            ProcessNames = processNames
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            AllowPublicTarget = flags.Contains("allow-public-target"),
            SelfTest = flags.Contains("self-test"),
            ShowHelp = flags.Contains("help") || flags.Contains("h")
        };

        options.Validate();
        return options;
    }

    public static string HelpText =>
        """
        NosGM Load Test

        Core options:
          --scenario tcp|login|world
          --host 127.0.0.1
          --port 1337
          --stages 100,250,500,750,1000,1250,1500
          --ramp-per-second 100
          --hold-seconds 30
          --connect-timeout-ms 5000
          --read-timeout-ms 5000
          --output artifacts/load-test/run-name
          --process-names NosGm.World,NosGm.Login,NosGm.Master.Server

        Login / World authentication:
          --accounts path/to/accounts.csv
          --region 5
          --client-version 0.9.3.3254
          --gameforge-id 0
          --client-data-old 0.9.3.3254
          --client-data 0.9.3.3254
          --login-template "NoS0575 0 {username} {password} {gameforgeId} {clientDataOld} {region} {clientData}"

        World scenario:
          --host 127.0.0.1              World host
          --port 1337                   World port
          --login-host 127.0.0.1        Login host (defaults to --host)
          --login-port 4000             Login port used to obtain SessionId
          --world-ready-packet finit    Late GameStart packet used as ready proof
          accounts.csv may use username,password,slot; slot defaults to 0.

        Safety / validation:
          --allow-public-target   Required for public Internet addresses.
          --self-test             Runs codec checks plus a 250-client loopback test.
          --help                  Shows this text.
        """;

    private void Validate()
    {
        if (Stages.Length == 0)
        {
            throw new ArgumentException("At least one load stage is required.");
        }

        int previous = 0;
        foreach (int stage in Stages)
        {
            if (stage <= previous)
            {
                throw new ArgumentException("Load stages must be strictly increasing.");
            }

            if (stage > 20000)
            {
                throw new ArgumentException("A single load generator run is capped at 20,000 clients.");
            }

            previous = stage;
        }

        if ((Scenario == LoadScenario.Login || Scenario == LoadScenario.World) && !SelfTest)
        {
            if (string.IsNullOrWhiteSpace(AccountsPath))
            {
                throw new ArgumentException("The login/world scenarios require --accounts <csv>.");
            }

            if (!File.Exists(AccountsPath))
            {
                throw new FileNotFoundException("The login accounts CSV was not found.", AccountsPath);
            }
        }

        if (Scenario == LoadScenario.World &&
            (WorldReadyPacket.Any(char.IsWhiteSpace) || string.IsNullOrWhiteSpace(WorldReadyPacket)))
        {
            throw new ArgumentException("--world-ready-packet must be a single packet header.");
        }
    }

    private static string Get(
        IReadOnlyDictionary<string, string> values,
        string key,
        string fallback) =>
        values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;

    private static string? GetOptional(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    private static int ParseInt(
        IReadOnlyDictionary<string, string> values,
        string key,
        int fallback,
        int minimum,
        int maximum)
    {
        string raw = Get(values, key, fallback.ToString(CultureInfo.InvariantCulture));
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ||
            parsed < minimum || parsed > maximum)
        {
            throw new ArgumentException($"--{key} must be between {minimum} and {maximum}.");
        }

        return parsed;
    }

    private static LoadScenario ParseScenario(string raw) => raw.ToLowerInvariant() switch
    {
        "tcp" => LoadScenario.Tcp,
        "login" => LoadScenario.Login,
        "world" => LoadScenario.World,
        _ => throw new ArgumentException("--scenario must be tcp, login or world.")
    };

    private static int[] ParseStages(string raw)
    {
        string[] parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var stages = new int[parts.Length];
        for (int index = 0; index < parts.Length; index++)
        {
            if (!int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                throw new ArgumentException($"Invalid stage '{parts[index]}'.");
            }

            stages[index] = parsed;
        }

        return stages;
    }
}

internal static class TargetSafety
{
    public static async Task EnsureAllowedAsync(
        string host,
        bool allowPublicTarget,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        IPAddress[] addresses = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
        if (addresses.Length == 0)
        {
            throw new InvalidOperationException($"Host '{host}' did not resolve to an IP address.");
        }

        bool hasPublicAddress = addresses.Any(address => !IsPrivateOrLoopback(address));
        if (hasPublicAddress && !allowPublicTarget)
        {
            throw new InvalidOperationException(
                "The target resolves to a public Internet address. Re-run with --allow-public-target only for a server you own or are authorized to test.");
        }
    }

    private static bool IsPrivateOrLoopback(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] bytes = address.GetAddressBytes();
            return bytes[0] == 10 ||
                   bytes[0] == 127 ||
                   bytes[0] == 192 && bytes[1] == 168 ||
                   bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
                   bytes[0] == 169 && bytes[1] == 254;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal ||
                   address.IsIPv6SiteLocal ||
                   (address.GetAddressBytes()[0] & 0xFE) == 0xFC;
        }

        return false;
    }
}
