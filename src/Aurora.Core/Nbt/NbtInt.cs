namespace Aurora.Core.Nbt;

public sealed class NbtInt : NbtTag
{
    public override NbtTagType Type => NbtTagType.Int;
    public int Value { get; }

    public NbtInt(int value)
    {
        Value = value;
    }
}
