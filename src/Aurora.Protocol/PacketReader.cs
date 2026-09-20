using System.Buffers;
using System.Text;
using System;

namespace Aurora.Protocol;

/// <summary>
/// Utility struct for reading primitive Minecraft types from a SequenceReader.
/// </summary>
public ref struct PacketReader
{
#pragma warning disable CA1051 // Visible instance fields are acceptable in high-performance ref structs
    public SequenceReader<byte> Reader;
#pragma warning restore CA1051

    public PacketReader(in ReadOnlySequence<byte> sequence)
    {
        Reader = new SequenceReader<byte>(sequence);
    }

    public int ReadVarInt()
    {
        if (!VarInt.TryRead(ref Reader, out int value, out _))
            throw new InvalidOperationException("Incomplete VarInt");
        return value;
    }

    public ushort ReadUShort()
    {
        if (!Reader.TryReadBigEndian(out short val))
            throw new InvalidOperationException("Incomplete UShort");
        return (ushort)val;
    }

    public short ReadShort()
    {
        if (!Reader.TryReadBigEndian(out short val))
            throw new InvalidOperationException("Incomplete Short");
        return val;
    }

    public int ReadInt()
    {
        if (!Reader.TryReadBigEndian(out int val))
            throw new InvalidOperationException("Incomplete Int");
        return val;
    }

    public string ReadString(int maxLength = 32767)
    {
        int length = ReadVarInt();
        if (length > maxLength * 4) throw new InvalidOperationException("String too long");
        
        ReadOnlySequence<byte> slice = Reader.Sequence.Slice(Reader.Position, length);
        Reader.Advance(length);

        if (slice.IsSingleSegment)
        {
            return Encoding.UTF8.GetString(slice.FirstSpan);
        }
        else
        {
            return Encoding.UTF8.GetString(slice.ToArray());
        }
    }
    public bool ReadBool()
    {
        if (!Reader.TryRead(out byte val))
            throw new InvalidOperationException("Incomplete Bool");
        return val != 0;
    }

    public byte ReadByte()
    {
        if (!Reader.TryRead(out byte val))
            throw new InvalidOperationException("Incomplete Byte");
        return val;
    }

    public long ReadLong()
    {
        if (!Reader.TryReadBigEndian(out long val))
            throw new InvalidOperationException("Incomplete Long");
        return val;
    }

    public double ReadDouble()
    {
        Span<byte> span = stackalloc byte[8];
        if (!Reader.TryCopyTo(span))
            throw new InvalidOperationException("Incomplete Double");
        Reader.Advance(8);
        return System.Buffers.Binary.BinaryPrimitives.ReadDoubleBigEndian(span);
    }

    public float ReadFloat()
    {
        Span<byte> span = stackalloc byte[4];
        if (!Reader.TryCopyTo(span))
            throw new InvalidOperationException("Incomplete Float");
        Reader.Advance(4);
        return System.Buffers.Binary.BinaryPrimitives.ReadSingleBigEndian(span);
    }

    public Guid ReadUUID()
    {
        ReadOnlySequence<byte> slice = Reader.Sequence.Slice(Reader.Position, 16);
        Reader.Advance(16);
        
        if (slice.IsSingleSegment)
        {
            var span = slice.FirstSpan;
            long msb = System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(span);
            long lsb = System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(span.Slice(8));
            return CreateGuid(msb, lsb);
        }
        else
        {
            Span<byte> temp = stackalloc byte[16];
            slice.CopyTo(temp);
            long msb = System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(temp);
            long lsb = System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(temp.Slice(8));
            return CreateGuid(msb, lsb);
        }
    }

    private static Guid CreateGuid(long msb, long lsb)
    {
        Span<byte> guidBytes = stackalloc byte[16];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(guidBytes, msb);
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(guidBytes.Slice(8), lsb);
        return new Guid(guidBytes, true); // Big-endian guid if we swap? Actually .NET Guid constructor from bytes expects little-endian for the first 3 fields, so it's safer to just return a string or properly parsed Guid. Wait, `new Guid(ReadOnlySpan<byte>, bool bigEndian)` exists in .NET 8/9!
    }

    public byte[] ReadByteArray()
    {
        int length = ReadVarInt();
        ReadOnlySequence<byte> slice = Reader.Sequence.Slice(Reader.Position, length);
        Reader.Advance(length);
        return slice.ToArray();
    }

    public Aurora.Core.Math.Coordinate ReadPosition()
    {
        long val = ReadLong();
        int x = (int)(val >> 38);
        int y = (int)((val << 52) >> 52);
        int z = (int)((val << 26) >> 38);
        return new Aurora.Core.Math.Coordinate(x, y, z);
    }
}
