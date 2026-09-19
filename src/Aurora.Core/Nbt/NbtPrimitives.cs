using System;
using System.Collections;
using System.Collections.Generic;

namespace Aurora.Core.Nbt;

public sealed class NbtByte : NbtTag
{
    public override NbtTagType Type => NbtTagType.Byte;
    public byte Value { get; set; }

    public NbtByte(byte value)
    {
        Value = value;
    }

    public NbtByte(sbyte value)
    {
        Value = (byte)value;
    }
}

public sealed class NbtShort : NbtTag
{
    public override NbtTagType Type => NbtTagType.Short;
    public short Value { get; set; }

    public NbtShort(short value)
    {
        Value = value;
    }
}

public sealed class NbtLong : NbtTag
{
    public override NbtTagType Type => NbtTagType.Long;
    public long Value { get; set; }

    public NbtLong(long value)
    {
        Value = value;
    }
}

public sealed class NbtFloat : NbtTag
{
    public override NbtTagType Type => NbtTagType.Float;
    public float Value { get; set; }

    public NbtFloat(float value)
    {
        Value = value;
    }
}

public sealed class NbtDouble : NbtTag
{
    public override NbtTagType Type => NbtTagType.Double;
    public double Value { get; set; }

    public NbtDouble(double value)
    {
        Value = value;
    }
}

public sealed class NbtString : NbtTag
{
    public override NbtTagType Type => NbtTagType.String;
    public string Value { get; set; }

    public NbtString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }
}

public sealed class NbtByteArray : NbtTag
{
    public override NbtTagType Type => NbtTagType.ByteArray;

#pragma warning disable CA1819 // NBT byte array requires raw array access for performance
    public byte[] Value { get; set; }
#pragma warning restore CA1819

    public NbtByteArray(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }
}

public sealed class NbtIntArray : NbtTag
{
    public override NbtTagType Type => NbtTagType.IntArray;

#pragma warning disable CA1819
    public int[] Value { get; set; }
#pragma warning restore CA1819

    public NbtIntArray(int[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }
}

public sealed class NbtLongArray : NbtTag
{
    public override NbtTagType Type => NbtTagType.LongArray;

#pragma warning disable CA1819
    public long[] Value { get; set; }
#pragma warning restore CA1819

    public NbtLongArray(long[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }
}

public sealed class NbtList : NbtTag, IEnumerable<NbtTag>
{
    public override NbtTagType Type => NbtTagType.List;
    public NbtTagType ElementType { get; private set; }
    
    private readonly List<NbtTag> _items = new();

    public int Count => _items.Count;

    public NbtList(NbtTagType elementType = NbtTagType.End)
    {
        ElementType = elementType;
    }

    public NbtTag this[int index] => _items[index];

    public void Add(NbtTag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        if (_items.Count == 0 && ElementType == NbtTagType.End)
        {
            ElementType = tag.Type;
        }
        else if (ElementType != tag.Type)
        {
            throw new InvalidOperationException($"Cannot add tag of type {tag.Type} to NbtList of {ElementType}");
        }

        _items.Add(tag);
    }

    public IEnumerator<NbtTag> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}
