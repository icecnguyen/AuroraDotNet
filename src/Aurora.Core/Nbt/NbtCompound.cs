using System;
using System.Collections;
using System.Collections.Generic;

namespace Aurora.Core.Nbt;

public sealed class NbtCompound : NbtTag, IEnumerable<KeyValuePair<string, NbtTag>>
{
    public override NbtTagType Type => NbtTagType.Compound;
    
    private readonly Dictionary<string, NbtTag> _tags = new(StringComparer.Ordinal);

    public int Count => _tags.Count;

    public NbtTag this[string name]
    {
        get => _tags[name];
        set
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(value);
            _tags[name] = value;
        }
    }

    public void Add(string name, NbtTag tag)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(tag);
        _tags[name] = tag;
    }
    
    public bool ContainsKey(string name)
    {
        return _tags.ContainsKey(name);
    }

    public bool TryGet(string name, out NbtTag? tag)
    {
        return _tags.TryGetValue(name, out tag);
    }

    public T? Get<T>(string name) where T : NbtTag
    {
        if (_tags.TryGetValue(name, out var tag) && tag is T typed)
        {
            return typed;
        }
        return null;
    }

    public byte GetByte(string name, byte defaultValue = 0)
    {
        return Get<NbtByte>(name)?.Value ?? defaultValue;
    }

    public short GetShort(string name, short defaultValue = 0)
    {
        return Get<NbtShort>(name)?.Value ?? defaultValue;
    }

    public int GetInt(string name, int defaultValue = 0)
    {
        return Get<NbtInt>(name)?.Value ?? defaultValue;
    }

    public long GetLong(string name, long defaultValue = 0)
    {
        return Get<NbtLong>(name)?.Value ?? defaultValue;
    }

    public string GetString(string name, string defaultValue = "")
    {
        return Get<NbtString>(name)?.Value ?? defaultValue;
    }

    public byte[]? GetByteArray(string name)
    {
        return Get<NbtByteArray>(name)?.Value;
    }

    public int[]? GetIntArray(string name)
    {
        return Get<NbtIntArray>(name)?.Value;
    }

    public long[]? GetLongArray(string name)
    {
        return Get<NbtLongArray>(name)?.Value;
    }

    public NbtCompound? GetCompound(string name)
    {
        return Get<NbtCompound>(name);
    }

    public NbtList? GetList(string name)
    {
        return Get<NbtList>(name);
    }

    public IEnumerator<KeyValuePair<string, NbtTag>> GetEnumerator() => _tags.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _tags.GetEnumerator();
}
