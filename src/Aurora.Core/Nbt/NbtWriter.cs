using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Aurora.Core.Nbt;

public static class NbtWriter
{
    public static void WriteRoot(Stream stream, NbtCompound root, string rootName = "")
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(rootName);

        stream.WriteByte((byte)NbtTagType.Compound);
        WriteStringValue(stream, rootName);
        WriteCompoundPayload(stream, root);
    }

    public static void WriteNamedTag(Stream stream, string name, NbtTag tag)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(tag);

        stream.WriteByte((byte)tag.Type);
        WriteStringValue(stream, name);
        WritePayload(stream, tag);
    }

    private static void WritePayload(Stream stream, NbtTag tag)
    {
        Span<byte> buffer8 = stackalloc byte[8];

        switch (tag.Type)
        {
            case NbtTagType.Byte:
            {
                var b = (NbtByte)tag;
                stream.WriteByte(b.Value);
                break;
            }
            case NbtTagType.Short:
            {
                var s = (NbtShort)tag;
                BinaryPrimitives.WriteInt16BigEndian(buffer8.Slice(0, 2), s.Value);
                stream.Write(buffer8.Slice(0, 2));
                break;
            }
            case NbtTagType.Int:
            {
                var i = (NbtInt)tag;
                BinaryPrimitives.WriteInt32BigEndian(buffer8.Slice(0, 4), i.Value);
                stream.Write(buffer8.Slice(0, 4));
                break;
            }
            case NbtTagType.Long:
            {
                var l = (NbtLong)tag;
                BinaryPrimitives.WriteInt64BigEndian(buffer8.Slice(0, 8), l.Value);
                stream.Write(buffer8.Slice(0, 8));
                break;
            }
            case NbtTagType.Float:
            {
                var f = (NbtFloat)tag;
                BinaryPrimitives.WriteSingleBigEndian(buffer8.Slice(0, 4), f.Value);
                stream.Write(buffer8.Slice(0, 4));
                break;
            }
            case NbtTagType.Double:
            {
                var d = (NbtDouble)tag;
                BinaryPrimitives.WriteDoubleBigEndian(buffer8.Slice(0, 8), d.Value);
                stream.Write(buffer8.Slice(0, 8));
                break;
            }
            case NbtTagType.ByteArray:
            {
                var ba = (NbtByteArray)tag;
                BinaryPrimitives.WriteInt32BigEndian(buffer8.Slice(0, 4), ba.Value.Length);
                stream.Write(buffer8.Slice(0, 4));
                stream.Write(ba.Value);
                break;
            }
            case NbtTagType.String:
            {
                var str = (NbtString)tag;
                WriteStringValue(stream, str.Value);
                break;
            }
            case NbtTagType.List:
            {
                var list = (NbtList)tag;
                stream.WriteByte((byte)list.ElementType);
                BinaryPrimitives.WriteInt32BigEndian(buffer8.Slice(0, 4), list.Count);
                stream.Write(buffer8.Slice(0, 4));
                foreach (var item in list)
                {
                    WritePayload(stream, item);
                }
                break;
            }
            case NbtTagType.Compound:
            {
                var compound = (NbtCompound)tag;
                WriteCompoundPayload(stream, compound);
                break;
            }
            case NbtTagType.IntArray:
            {
                var ia = (NbtIntArray)tag;
                BinaryPrimitives.WriteInt32BigEndian(buffer8.Slice(0, 4), ia.Value.Length);
                stream.Write(buffer8.Slice(0, 4));
                Span<byte> intBuf = stackalloc byte[4];
                foreach (int val in ia.Value)
                {
                    BinaryPrimitives.WriteInt32BigEndian(intBuf, val);
                    stream.Write(intBuf);
                }
                break;
            }
            case NbtTagType.LongArray:
            {
                var la = (NbtLongArray)tag;
                BinaryPrimitives.WriteInt32BigEndian(buffer8.Slice(0, 4), la.Value.Length);
                stream.Write(buffer8.Slice(0, 4));
                Span<byte> longBuf = stackalloc byte[8];
                foreach (long val in la.Value)
                {
                    BinaryPrimitives.WriteInt64BigEndian(longBuf, val);
                    stream.Write(longBuf);
                }
                break;
            }
            default:
                throw new InvalidOperationException($"Cannot write unsupported NBT tag type: {tag.Type}");
        }
    }

    private static void WriteCompoundPayload(Stream stream, NbtCompound compound)
    {
        foreach (var pair in compound)
        {
            WriteNamedTag(stream, pair.Key, pair.Value);
        }
        // Write Tag_End
        stream.WriteByte((byte)NbtTagType.End);
    }

    private static void WriteStringValue(Stream stream, string value)
    {
        byte[] strBytes = Encoding.UTF8.GetBytes(value);
        if (strBytes.Length > ushort.MaxValue)
        {
            throw new ArgumentException($"String length ({strBytes.Length}) exceeds maximum NBT string length ({ushort.MaxValue}).");
        }

        Span<byte> lenBuf = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(lenBuf, (ushort)strBytes.Length);
        stream.Write(lenBuf);
        if (strBytes.Length > 0)
        {
            stream.Write(strBytes);
        }
    }
}
