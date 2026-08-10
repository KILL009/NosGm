using System.Globalization;
using System.Text;

namespace NosGM.LoadTest;

internal sealed record LoadAccount(string Username, string Password, byte Slot = 0);

internal static class LoadAccountReader
{
    public static IReadOnlyList<LoadAccount> Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var accounts = new List<LoadAccount>();
        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            string[] fields = line.Split(',', 3, StringSplitOptions.TrimEntries);
            if (fields.Length is < 2 or > 3)
            {
                throw new FormatException(
                    $"Invalid account row '{line}'. Expected username,password[,slot].");
            }

            if (string.Equals(fields[0], "username", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(fields[1], "password", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(fields[0]) || string.IsNullOrEmpty(fields[1]))
            {
                throw new FormatException("Load-test accounts cannot have an empty username or password.");
            }

            byte slot = 0;
            if (fields.Length == 3 &&
                (!byte.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out slot) || slot > 3))
            {
                throw new FormatException(
                    $"Invalid character slot '{fields[2]}' for account '{fields[0]}'. Expected 0..3.");
            }

            accounts.Add(new LoadAccount(fields[0], fields[1], slot));
        }

        if (accounts.Count == 0)
        {
            throw new InvalidOperationException("The account CSV does not contain any accounts.");
        }

        if (accounts.Select(account => account.Username)
            .Distinct(StringComparer.Ordinal)
            .Count() != accounts.Count)
        {
            throw new InvalidOperationException("Every load-test account username must be unique.");
        }

        return accounts;
    }
}

internal static class NosTaleLoginCodec
{
    private const byte LoginXorKey = 195;
    private const int LoginOffset = 15;
    private const byte ServerPacketTerminator = 25;

    public static string BuildPacket(
        LoadTestOptions options,
        LoadAccount account,
        int clientIndex)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(account);

        return options.LoginPacketTemplate
            .Replace("{index}", clientIndex.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{username}", account.Username, StringComparison.Ordinal)
            .Replace("{password}", account.Password, StringComparison.Ordinal)
            .Replace("{gameforgeId}", options.GameforgeId, StringComparison.Ordinal)
            .Replace("{clientDataOld}", options.ClientDataOld, StringComparison.Ordinal)
            .Replace("{region}", options.Region.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{clientData}", options.ClientData, StringComparison.Ordinal)
            .Replace("{clientVersion}", options.ClientVersion, StringComparison.Ordinal);
    }

    public static byte[] EncodeClientPacket(string packet)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packet);

        byte[] clear = Encoding.ASCII.GetBytes(packet);
        var encrypted = new byte[clear.Length];
        for (int index = 0; index < clear.Length; index++)
        {
            int transformed = (clear[index] ^ LoginXorKey) + LoginOffset;
            encrypted[index] = unchecked((byte)transformed);
        }

        return encrypted;
    }

    public static string DecodeServerPacket(ReadOnlySpan<byte> encrypted)
    {
        if (encrypted.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(encrypted.Length);
        foreach (byte value in encrypted)
        {
            if (value == ServerPacketTerminator)
            {
                break;
            }

            builder.Append((char)unchecked((byte)(value - LoginOffset)));
        }

        return builder.ToString();
    }

    public static bool LooksAccepted(string response) =>
        !string.IsNullOrWhiteSpace(response) &&
        !response.StartsWith("failc", StringComparison.OrdinalIgnoreCase) &&
        (response.Contains("NsTeST", StringComparison.OrdinalIgnoreCase) ||
         response.Contains(':'));

    public static bool TryParseWorldTicket(
        string response,
        out int sessionId,
        out string? advertisedHost,
        out int advertisedPort)
    {
        sessionId = 0;
        advertisedHost = null;
        advertisedPort = 0;
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        string[] tokens = response.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int nsTestIndex = Array.FindIndex(
            tokens,
            token => string.Equals(token, "NsTeST", StringComparison.OrdinalIgnoreCase));
        if (nsTestIndex < 0)
        {
            return false;
        }

        for (int index = nsTestIndex + 2; index < tokens.Length; index++)
        {
            if (!TryParseAdvertisedEndpoint(tokens[index], out string? host, out int port))
            {
                continue;
            }

            if (index <= nsTestIndex + 1 ||
                !int.TryParse(tokens[index - 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedSessionId) ||
                parsedSessionId <= 0)
            {
                return false;
            }

            sessionId = parsedSessionId;
            advertisedHost = host;
            advertisedPort = port;
            return true;
        }

        return false;
    }

    public static void RunSelfTest()
    {
        const string response =
            "NsTeST 5 load001 -99 0 -99 0 -99 0 -99 0 -99 0 424242 127.0.0.1:1337:1:1.1.Sumeria -1:-1:-1:10000.10000.1";
        if (!TryParseWorldTicket(response, out int sessionId, out string? host, out int port) ||
            sessionId != 424242 ||
            !string.Equals(host, "127.0.0.1", StringComparison.Ordinal) ||
            port != 1337)
        {
            throw new InvalidOperationException("Login -> World ticket parser self-test failed.");
        }
    }

    private static bool TryParseAdvertisedEndpoint(string token, out string? host, out int port)
    {
        host = null;
        port = 0;
        if (string.IsNullOrWhiteSpace(token) || token.StartsWith("-1:", StringComparison.Ordinal))
        {
            return false;
        }

        string[] parts = token.Split(':');
        if (parts.Length < 4 || string.IsNullOrWhiteSpace(parts[0]) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out port) ||
            port is < 1 or > 65535)
        {
            port = 0;
            return false;
        }

        host = parts[0];
        return true;
    }
}
