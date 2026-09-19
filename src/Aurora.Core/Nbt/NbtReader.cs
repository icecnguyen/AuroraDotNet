using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Aurora.Core.Nbt;

public static class NbtReader
{
    public static NbtCompound ReadRoot(Stream stream)
    {
        return ReadRoot(stream, out _);
    }

    public static NbtCompound ReadRoot(Stream stream, out string rootName)
    {
        ArgumentNullException.ThrowIfNull(stream);

        int typeByte = stream.ReadByte();
        if (typeByte < 0)
        {
            throw new EndOfStreamException("Unexpected end of stream while reading NBT root tag type.");
        }

        var tagType = (NbtTagType)typeByte;
        if (tagType != NbtTagType.Compound)
        {
            throw new InvalidDataException($"Expected root tag to be Compound (10), got {tagType} ({typeByte}).");
        }

        rootName = ReadStringValue(stream);
        var rootTag = ReadCompoundPayload(stream);
        return rootTag;
    }

    public static NbtTag? ReadTag(Stream stream, out string tagName)
    {
        ArgumentNullException.ThrowIfNull(stream);

        int typeByte = stream.ReadByte();
        if (typeByte <= 0)
        {
            tagName = string.Empty;
            return null; // Tag_End or EOF
        }

        var tagType = (NbtTagType)typeByte;
        tagName = ReadStringValue(stream);
        return ReadPayload(stream, tagType);
    }

    private static NbtTag ReadPayload(Stream stream, NbtTagType type)
    {
        Span<byte> buffer8 = stackalloc byte[8];

        switch (type)
        {
            case NbtTagType.Byte:
            {
                int b = stream.ReadByte();
                if (b < 0) throw new EndOfStreamException();
                return new NbtByte((byte)b);
            }
            case NbtTagType.Short:
            {
                ReadExact(stream, buffer8.Slice(0, 2));
                return new NbtShort(BinaryPrimitives.ReadInt16BigEndian(buffer8.Slice(0, 2)));
            }
            case NbtTagType.Int:
            {
                ReadExact(stream, buffer8.Slice(0, 4));
                return new NbtInt(BinaryPrimitives.ReadInt32BigEndian(buffer8.Slice(0, 4)));
            }
            case NbtTagType.Long:
            {
                ReadExact(stream, buffer8.Slice(0, 8));
                return new NbtLong(BinaryPrimitives.ReadInt64BigEndian(buffer8.Slice(0, 8)));
            }
            case NbtTagType.Float:
            {
                ReadExact(stream, buffer8.Slice(0, 4));
                return new NbtFloat(BinaryPrimitives.ReadSingleBigEndian(buffer8.Slice(0, 4)));
            }
            case NbtTagType.Double:
            {
                ReadExact(stream, buffer8.Slice(0, 8));
                return new NbtDouble(BinaryPrimitives.ReadDoubleBigEndian(buffer8.Slice(0, 8)));
            }
            case NbtTagType.ByteArray:
            {
                ReadExact(stream, buffer8.Slice(0, 4));
                int length = BinaryPrimitives.ReadInt32BigEndian(buffer8.Slice(0, 4));
                if (length < 0) throw new InvalidDataException($"Negative ByteArray length: {length}");
                byte[] data = new byte[length];
                ReadExact(stream, data);
                return new NbtByteArray(data);
            }
            case NbtTagType.String:
            {
                return new NbtString(ReadStringValue(stream));
            }
            case NbtTagType.List:
            {
                int elemTypeByte = stream.ReadByte();
                if (elemTypeByte < 0) throw new EndOfStreamException();
                var elemType = (NbtTagType)elemTypeByte;

                ReadExact(stream, buffer8.Slice(0, 4));
                int length = BinaryPrimitives.ReadInt32BigEndian(buffer8.Slice(0, 4));
                if (length < 0) throw new InvalidDataException($"Negative List length: {length}");

                var list = new NbtList(elemType);
                for (int i = 0; i < length; i++)
                {
                    list.Add(ReadPayload(stream, elemType));
                }
                return list;
            }
            case NbtTagType.Compound:
            {
                return ReadCompoundPayload(stream);
            }
            case NbtTagType.IntArray:
            {
                ReadExact(stream, buffer8.Slice(0, 4));
                int length = BinaryPrimitives.ReadInt32BigEndian(buffer8.Slice(0, 4));
                if (length < 0) throw new InvalidDataException($"Negative IntArray length: {length}");
                int[] data = new int[length];
                Span<byte> intBuf = stackalloc byte[4];
                for (int i = 0; i < length; i++)
                {
                    ReadExact(stream, intBuf);
                    data[i] = BinaryPrimitives.ReadInt32BigEndian(intBuf);
                }
                return new NbtIntArray(data);
            }
            case NbtTagType.LongArray:
            {
                ReadExact(stream, buffer8.Slice(0, 4));
                int length = BinaryPrimitives.ReadInt32BigEndian(buffer8.Slice(0, 4));
                if (length < 0) throw new InvalidDataException($"Negative LongArray length: {length}");
                long[] data = new long[length];
                Span<byte> longBuf = stackalloc byte[8];
                for (int i = 0; i < length; i++)
                {
                    ReadExact(stream, longBuf);
                    data[i] = BinaryPrimitives.ReadInt64BigEndian(longBuf);
                }
                return new NbtLongArray(data);
            }
            default:
                throw new InvalidDataException($"Unknown NBT tag type: {type}");
        }
    }

    private static NbtCompound ReadCompoundPayload(Stream stream)
    {
        var compound = new NbtCompound();
        while (true)
        {
            var tag = ReadTag(stream, out string name);
            if (tag == null)
            {
                break; // End tag encountered
            }
            compound.Add(name, tag);
        }
        return compound;
    }

    private static string ReadStringValue(Stream stream)
    {
        Span<byte> lenBuf = stackalloc byte[2];
        ReadExact(stream, lenBuf);
        ushort length = BinaryPrimitives.ReadUInt16BigEndian(lenBuf);
        if (length == 0) return string.Empty;

        byte[] strBytes = new byte[length];
        ReadExact(stream, strBytes);
        return Encoding.UTF8.GetString(strBytes);
    }

    private static void ReadExact(Stream stream, Span<byte> buffer)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = stream.Read(buffer.Slice(totalRead));
            if (read == 0)
            {
                throw new EndOfStreamException($"Expected {buffer.Length} bytes, but reached EOF after {totalRead} bytes.");
            }
            totalRead += read;
        }
    }
}
