using System;
using System.Collections.Generic;

namespace Aurora.Core.Nbt;

public sealed class NbtCompound : NbtTag
{
    public override NbtTagType Type => NbtTagType.Compound;
    
    private readonly Dictionary<string, NbtTag> _tags = new(StringComparer.Ordinal);
    
    public void Add(string name, NbtTag tag)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(tag);
        _tags[name] = tag;
    }
    
    public bool TryGet(string name, out NbtTag? tag)
    {
        return _tags.TryGetValue(name, out tag);
    }
}
