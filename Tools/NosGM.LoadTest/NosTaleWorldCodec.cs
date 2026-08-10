using System.Text;

namespace NosGM.LoadTest;

internal static class NosTaleWorldCodec
{
    private const byte InitialTerminator = 0x0E;
    private const byte InitialOffset = 0x0F;
    private const byte PacketSeparator = 0xFF;
    private const byte PacketXor = 0xC3;
    private const int MaximumPlainChunk = 0x7A;

    public static byte[] EncodeInitialHandshake(int sessionId)
    {
        if (sessionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionId), "World SessionId must be positive.");
        }

        string clear = $"0 {sessionId}";
        var result = new List<byte>(clear.Length / 2 + 3)
        {
            0
        };

        for (int index = 0; index < clear.Length; index += 2)
        {
            int high = EncodeInitialNibble(clear[index]);
            int low = index + 1 < clear.Length
                ? EncodeInitialNibble(clear[index + 1])
                : 0;
            int packed = (high << 4) | low;
            result.Add(unchecked((byte)(packed + InitialOffset)));
        }

        result.Add(InitialTerminator);
        return result.ToArray();
    }

    public static byte[] EncodeClientPacket(int sessionId, string packet)
    {
        if (sessionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionId), "World SessionId must be positive.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(packet);

        byte[] clear = Encoding.ASCII.GetBytes(packet);
        var transformed = new List<byte>(clear.Length + clear.Length / MaximumPlainChunk + 2);

        for (int offset = 0; offset < clear.Length; offset += MaximumPlainChunk)
        {
            int length = Math.Min(MaximumPlainChunk, clear.Length - offset);
            transformed.Add((byte)length);
            for (int index = 0; index < length; index++)
            {
                transformed.Add(unchecked((byte)(clear[offset + index] ^ 0xFF)));
            }
        }

        transformed.Add(PacketSeparator);

        byte sessionKey = unchecked((byte)(sessionId & 0xFF));
        byte firstByte = unchecked((byte)(sessionKey + 0x40));
        int sessionNumber = (sessionId >> 6) & 0x03;
        var encrypted = new byte[transformed.Count];

        for (int index = 0; index < transformed.Count; index++)
        {
            byte value = transformed[index];
            encrypted[index] = sessionNumber switch
            {
                0 => unchecked((byte)(value + firstByte)),
                1 => unchecked((byte)(value - firstByte)),
                2 => unchecked((byte)((value ^ PacketXor) + firstByte)),
                3 => unchecked((byte)((value ^ PacketXor) - firstByte)),
                _ => throw new InvalidOperationException("Invalid World session transform.")
            };
        }

        return encrypted;
    }

    public static void RunSelfTest()
    {
        const int sessionId = 123456789;
        byte[] initial = EncodeInitialHandshake(sessionId);
        string decodedInitial = DecodeInitialForSelfTest(initial);
        if (!string.Equals(decodedInitial, $"0 {sessionId}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"World initial-handshake codec self-test failed: '{decodedInitial}'.");
        }

        const string packet = "3 select 0";
        byte[] encrypted = EncodeClientPacket(sessionId, packet);
        string decodedPacket = DecodeClientForSelfTest(sessionId, encrypted);
        if (!string.Equals(decodedPacket, packet, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"World client-packet codec self-test failed: '{decodedPacket}'.");
        }

        var reader = new NosTaleWorldPacketReader();
        byte[] serverFrame = EncodeServerForSelfTest("finit 1 2 3");
        int split = serverFrame.Length / 2;
        reader.Append(serverFrame.AsSpan(0, split));
        if (reader.DrainPackets().Count != 0)
        {
            throw new InvalidOperationException("World server-packet reader emitted a partial frame.");
        }

        reader.Append(serverFrame.AsSpan(split));
        IReadOnlyList<string> packets = reader.DrainPackets();
        if (packets.Count != 1 || !string.Equals(packets[0], "finit 1 2 3", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("World server-packet reader self-test failed.");
        }
    }

    private static int EncodeInitialNibble(char value) => value switch
    {
        ' ' => 0,
        '-' => 2,
        '.' => 3,
        _ when value >= '0' && value <= '9' => value - 0x2C,
        _ => throw new ArgumentException(
            $"Character '{value}' is not valid in the initial World custom parameter.")
    };

    private static string DecodeInitialForSelfTest(ReadOnlySpan<byte> data)
    {
        var builder = new StringBuilder();
        for (int index = 1; index < data.Length; index++)
        {
            if (data[index] == InitialTerminator)
            {
                break;
            }

            int packed = data[index] - InitialOffset;
            AppendInitialNibble(builder, (packed >> 4) & 0x0F);
            AppendInitialNibble(builder, packed & 0x0F);
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendInitialNibble(StringBuilder builder, int nibble)
    {
        switch (nibble)
        {
            case 0:
            case 1:
                builder.Append(' ');
                break;
            case 2:
                builder.Append('-');
                break;
            case 3:
                builder.Append('.');
                break;
            default:
                builder.Append((char)(nibble + 0x2C));
                break;
        }
    }

    private static string DecodeClientForSelfTest(int sessionId, ReadOnlySpan<byte> encrypted)
    {
        byte sessionKey = unchecked((byte)(sessionId & 0xFF));
        byte firstByte = unchecked((byte)(sessionKey + 0x40));
        int sessionNumber = (sessionId >> 6) & 0x03;
        var transformed = new byte[encrypted.Length];

        for (int index = 0; index < encrypted.Length; index++)
        {
            byte value = encrypted[index];
            transformed[index] = sessionNumber switch
            {
                0 => unchecked((byte)(value - firstByte)),
                1 => unchecked((byte)(value + firstByte)),
                2 => unchecked((byte)((value - firstByte) ^ PacketXor)),
                3 => unchecked((byte)((value + firstByte) ^ PacketXor)),
                _ => throw new InvalidOperationException("Invalid World session transform.")
            };
        }

        int separator = Array.IndexOf(transformed, PacketSeparator);
        ReadOnlySpan<byte> encoded = separator >= 0
            ? transformed.AsSpan(0, separator)
            : transformed.AsSpan();
        var clear = new List<byte>(encoded.Length);

        int cursor = 0;
        while (cursor < encoded.Length)
        {
            int length = encoded[cursor++];
            if (length > MaximumPlainChunk || cursor + length > encoded.Length)
            {
                throw new InvalidOperationException("Invalid World load-test encoded packet.");
            }

            for (int index = 0; index < length; index++)
            {
                clear.Add(unchecked((byte)(encoded[cursor++] ^ 0xFF)));
            }
        }

        return Encoding.ASCII.GetString(clear.ToArray());
    }

    private static byte[] EncodeServerForSelfTest(string packet)
    {
        byte[] clear = Encoding.ASCII.GetBytes(packet);
        var result = new List<byte>(clear.Length + clear.Length / 0x7E + 2);
        for (int offset = 0; offset < clear.Length; offset += 0x7E)
        {
            int length = Math.Min(0x7E, clear.Length - offset);
            result.Add((byte)length);
            for (int index = 0; index < length; index++)
            {
                result.Add(unchecked((byte)~clear[offset + index]));
            }
        }
        result.Add(PacketSeparator);
        return result.ToArray();
    }
}

internal sealed class NosTaleWorldPacketReader
{
    private readonly List<byte> _buffer = [];
    private readonly Queue<string> _packets = new();

    public void Append(ReadOnlySpan<byte> data)
    {
        for (int index = 0; index < data.Length; index++)
        {
            byte value = data[index];
            if (value == 0xFF)
            {
                if (_buffer.Count > 0)
                {
                    _packets.Enqueue(DecodeFrame(_buffer));
                    _buffer.Clear();
                }
                continue;
            }

            _buffer.Add(value);
        }
    }

    public IReadOnlyList<string> DrainPackets()
    {
        if (_packets.Count == 0)
        {
            return Array.Empty<string>();
        }

        var result = new string[_packets.Count];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = _packets.Dequeue();
        }
        return result;
    }

    private static string DecodeFrame(IReadOnlyList<byte> frame)
    {
        var clear = new List<byte>(frame.Count);
        int cursor = 0;
        while (cursor < frame.Count)
        {
            int length = frame[cursor++];
            if (length <= 0 || cursor + length > frame.Count)
            {
                throw new InvalidDataException("Malformed NosTale World server frame.");
            }

            for (int index = 0; index < length; index++)
            {
                clear.Add(unchecked((byte)~frame[cursor++]));
            }
        }

        return Encoding.ASCII.GetString(clear.ToArray());
    }
}
