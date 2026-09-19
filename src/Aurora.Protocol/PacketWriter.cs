using System.Buffers;
using System.Text;
using System;

namespace Aurora.Protocol;

/// <summary>
/// Utility ref struct for writing primitive Minecraft types to an IBufferWriter.
/// </summary>
public ref struct PacketWriter
{
#pragma warning disable CA1051 // Visible instance fields are acceptable in high-performance ref structs
    public IBufferWriter<byte> Writer;
#pragma warning restore CA1051

    public PacketWriter(IBufferWriter<byte> writer)
    {
        Writer = writer;
    }

    public void WriteVarInt(int value)
    {
        VarInt.Write(Writer, value);
    }

    public void WriteString(string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        WriteVarInt(byteCount);
        
        var span = Writer.GetSpan(byteCount);
        Encoding.UTF8.GetBytes(value, span);
        Writer.Advance(byteCount);
    }

    public void WriteUShort(ushort value)
    {
        var span = Writer.GetSpan(2);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(span, value);
        Writer.Advance(2);
    }

    public void WriteShort(short value)
    {
        var span = Writer.GetSpan(2);
        System.Buffers.Binary.BinaryPrimitives.WriteInt16BigEndian(span, value);
        Writer.Advance(2);
    }
    public void WriteBool(bool value)
    {
        var span = Writer.GetSpan(1);
        span[0] = (byte)(value ? 1 : 0);
        Writer.Advance(1);
    }

    public void WriteLong(long value)
    {
        var span = Writer.GetSpan(8);
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(span, value);
        Writer.Advance(8);
    }

    public void WriteDouble(double value)
    {
        var span = Writer.GetSpan(8);
        System.Buffers.Binary.BinaryPrimitives.WriteDoubleBigEndian(span, value);
        Writer.Advance(8);
    }

    public void WriteFloat(float value)
    {
        var span = Writer.GetSpan(4);
        System.Buffers.Binary.BinaryPrimitives.WriteSingleBigEndian(span, value);
        Writer.Advance(4);
    }

    public void WriteInt(int value)
    {
        var span = Writer.GetSpan(4);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(span, value);
        Writer.Advance(4);
    }

    public void WriteByte(byte value)
    {
        var span = Writer.GetSpan(1);
        span[0] = value;
        Writer.Advance(1);
    }

    public void WriteUUID(Guid value)
    {
        var span = Writer.GetSpan(16);
        value.TryWriteBytes(span, true, out _); // bigEndian = true in .NET 8/9
        Writer.Advance(16);
    }

    public void WriteByteArray(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        WriteVarInt(data.Length);
        var span = Writer.GetSpan(data.Length);
        data.CopyTo(span);
        Writer.Advance(data.Length);
    }
}
