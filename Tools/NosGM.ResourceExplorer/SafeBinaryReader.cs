// SPDX-License-Identifier: BSL-1.0

using System.Buffers.Binary;

namespace NosGM.ResourceExplorer;

internal sealed class SafeBinaryReader
{
    private readonly byte[] _data;

    public SafeBinaryReader(byte[] data) => _data = data;

    public int Length => _data.Length;
    public int Position { get; private set; }
    public int Remaining => Length - Position;

    public void Seek(int position, string field)
    {
        if (position < 0 || position > Length)
        {
            throw new InvalidDataException($"{field} points outside the file: {position} of {Length} bytes.");
        }
        Position = position;
    }

    public byte ReadByte(string field)
    {
        EnsureAvailable(1, field);
        return _data[Position++];
    }

    public int ReadInt32(string field)
    {
        EnsureAvailable(4, field);
        var value = BinaryPrimitives.ReadInt32LittleEndian(_data.AsSpan(Position, 4));
        Position += 4;
        return value;
    }

    public byte[] ReadBytes(int count, string field)
    {
        if (count < 0)
        {
            throw new InvalidDataException($"{field} has a negative size: {count}.");
        }
        EnsureAvailable(count, field);
        var result = _data.AsSpan(Position, count).ToArray();
        Position += count;
        return result;
    }

    public byte[] Slice(int offset, int count, string field)
    {
        if (offset < 0 || count < 0 || (long)offset + count > Length)
        {
            throw new InvalidDataException($"{field} range is outside the file: offset={offset}, size={count}, file={Length}.");
        }
        return _data.AsSpan(offset, count).ToArray();
    }

    private void EnsureAvailable(int count, string field)
    {
        if (count < 0 || (long)Position + count > Length)
        {
            throw new InvalidDataException($"{field} needs {count} bytes at offset {Position}, but only {Remaining} remain.");
        }
    }
}
