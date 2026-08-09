using System.Text;

namespace NosGM.LoadTest;

internal sealed record LoadAccount(string Username, string Password);

internal static class LoadAccountReader
{
    public static IReadOnlyList<LoadAccount> Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var accounts = new List<LoadAccount>();
        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            string[] fields = line.Split(',', 2, StringSplitOptions.TrimEntries);
            if (fields.Length != 2)
            {
                throw new FormatException(
                    $"Invalid account row '{line}'. Expected username,password.");
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

            accounts.Add(new LoadAccount(fields[0], fields[1]));
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
            .Replace("{index}", clientIndex.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{username}", account.Username, StringComparison.Ordinal)
            .Replace("{password}", account.Password, StringComparison.Ordinal)
            .Replace("{gameforgeId}", options.GameforgeId, StringComparison.Ordinal)
            .Replace("{clientDataOld}", options.ClientDataOld, StringComparison.Ordinal)
            .Replace("{region}", options.Region.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{clientData}", options.ClientData, StringComparison.Ordinal)
            .Replace("{clientVersion}", options.ClientVersion, StringComparison.Ordinal);
    }

    /// <summary>
    /// Inverse of NosGm.Core.LoginCryptography.Decrypt. This is the transform
    /// used by a client when sending an ASCII login packet to NosGM.
    /// </summary>
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

    /// <summary>
    /// Decodes server-to-login-client packets produced by
    /// NosGm.Core.LoginCryptography.Encrypt. The final byte 25 is a terminator.
    /// </summary>
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
         response.Contains(':', StringComparison.Ordinal));
}
