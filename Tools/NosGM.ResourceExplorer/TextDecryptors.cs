// SPDX-License-Identifier: BSL-1.0
// Ported from OnexExplorer's NosTextDatFileDecryptor and NosTextOthersFileDecryptor.

using System.Buffers.Binary;

namespace NosGM.ResourceExplorer;

internal static class TextDecryptors
{
    private static readonly byte[] Crypto = [0x00, 0x20, 0x2D, 0x2E, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x0A, 0x00];

    public static byte[] DecryptDat(byte[] input, int maxOutput)
    {
        using var output = new MemoryStream();
        var position = 0;
        while (position < input.Length)
        {
            var current = input[position++];
            if (current == 0xFF)
            {
                WriteChecked(output, 0x0D, maxOutput);
                continue;
            }

            var count = current & 0x7F;
            if ((current & 0x80) != 0)
            {
                for (var remaining = count; remaining > 0; remaining -= 2)
                {
                    if (position >= input.Length)
                    {
                        throw new InvalidDataException("A DAT compact sequence is truncated.");
                    }
                    current = input[position++];
                    WriteChecked(output, Crypto[(current & 0xF0) >> 4], maxOutput);
                    if (remaining <= 1)
                    {
                        break;
                    }
                    var second = Crypto[current & 0x0F];
                    if (second == 0)
                    {
                        break;
                    }
                    WriteChecked(output, second, maxOutput);
                }
            }
            else
            {
                for (var remaining = count; remaining > 0; remaining--)
                {
                    if (position >= input.Length)
                    {
                        throw new InvalidDataException("A DAT XOR sequence is truncated.");
                    }
                    WriteChecked(output, (byte)(input[position++] ^ 0x33), maxOutput);
                }
            }
        }
        return output.ToArray();
    }

    public static byte[] DecryptList(byte[] input, int maxOutput)
    {
        if (input.Length < 4)
        {
            throw new InvalidDataException("A list entry is too short.");
        }
        var lineCount = BinaryPrimitives.ReadInt32LittleEndian(input.AsSpan(0, 4));
        if (lineCount < 0 || lineCount > 5_000_000)
        {
            throw new InvalidDataException($"List line count {lineCount} is outside the allowed range.");
        }

        using var output = new MemoryStream();
        var position = 4;
        for (var line = 0; line < lineCount; line++)
        {
            if (position + 4 > input.Length)
            {
                throw new InvalidDataException($"List line {line} length is truncated.");
            }
            var length = BinaryPrimitives.ReadInt32LittleEndian(input.AsSpan(position, 4));
            position += 4;
            if (length < 0 || position + (long)length > input.Length)
            {
                throw new InvalidDataException($"List line {line} has an invalid length: {length}.");
            }
            if (output.Length + length + 1 > maxOutput)
            {
                throw new InvalidDataException("List decoding exceeds the configured output limit.");
            }
            for (var i = 0; i < length; i++)
            {
                output.WriteByte((byte)(input[position + i] ^ 0x01));
            }
            output.WriteByte((byte)'\n');
            position += length;
        }
        if (position != input.Length)
        {
            throw new InvalidDataException($"List decoding left {input.Length - position} unexpected bytes.");
        }
        return output.ToArray();
    }

    private static void WriteChecked(Stream output, byte value, int maxOutput)
    {
        if (output.Length >= maxOutput)
        {
            throw new InvalidDataException("DAT decoding exceeds the configured output limit.");
        }
        output.WriteByte(value);
    }
}
