namespace Aurora.Core.Nbt;

/// <summary>
/// Base class for all NBT tags.
/// </summary>
public abstract class NbtTag
{
    public abstract NbtTagType Type { get; }
}
