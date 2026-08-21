namespace Aurora.Core.Nbt;

#pragma warning disable CA1028 // NBT specification uses a single byte for type ID
#pragma warning disable CA1720 // NBT specification names exactly match primitive types
public enum NbtTagType : byte
{
    End = 0,
    Byte = 1,
    Short = 2,
    Int = 3,
    Long = 4,
    Float = 5,
    Double = 6,
    ByteArray = 7,
    String = 8,
    List = 9,
    Compound = 10,
    IntArray = 11,
    LongArray = 12
}
#pragma warning restore CA1720
#pragma warning restore CA1028
