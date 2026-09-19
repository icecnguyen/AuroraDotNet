using System.IO;
using Aurora.Core.Nbt;
using Xunit;

namespace Aurora.Core.Tests;

public class NbtTests
{
    [Fact]
    public void NbtCompoundStoresAndRetrievesTags()
    {
        var compound = new NbtCompound();
        var intTag = new NbtInt(42);
        
        compound.Add("Age", intTag);
        
        Assert.True(compound.TryGet("Age", out var retrieved));
        Assert.NotNull(retrieved);
        Assert.Equal(NbtTagType.Int, retrieved.Type);
        
        var retrievedInt = Assert.IsType<NbtInt>(retrieved);
        Assert.Equal(42, retrievedInt.Value);
    }

    [Fact]
    public void NbtStreamRoundtripAllTypes()
    {
        var root = new NbtCompound();
        root.Add("ByteVal", new NbtByte(0x7F));
        root.Add("ShortVal", new NbtShort(12345));
        root.Add("IntVal", new NbtInt(12345678));
        root.Add("LongVal", new NbtLong(987654321012345L));
        root.Add("FloatVal", new NbtFloat(3.14159f));
        root.Add("DoubleVal", new NbtDouble(2.718281828459));
        root.Add("StringVal", new NbtString("Minecraft 1.21.4 Anvil Parity"));
        root.Add("ByteArrayVal", new NbtByteArray(new byte[] { 1, 2, 3, 4, 5, 255 }));
        root.Add("IntArrayVal", new NbtIntArray(new int[] { 100, 200, -300, 400 }));
        root.Add("LongArrayVal", new NbtLongArray(new long[] { 1000L, -2000L, 3000L }));

        var list = new NbtList(NbtTagType.String);
        list.Add(new NbtString("Entry1"));
        list.Add(new NbtString("Entry2"));
        root.Add("ListVal", list);

        var innerCompound = new NbtCompound();
        innerCompound.Add("InnerInt", new NbtInt(999));
        root.Add("InnerComp", innerCompound);

        using var ms = new MemoryStream();
        NbtWriter.WriteRoot(ms, root, "RootTag");

        ms.Position = 0;
        var readRoot = NbtReader.ReadRoot(ms, out string rootName);

        Assert.Equal("RootTag", rootName);
        Assert.Equal((byte)0x7F, readRoot.GetByte("ByteVal"));
        Assert.Equal(12345, readRoot.GetShort("ShortVal"));
        Assert.Equal(12345678, readRoot.GetInt("IntVal"));
        Assert.Equal(987654321012345L, readRoot.GetLong("LongVal"));
        Assert.Equal(3.14159f, readRoot.Get<NbtFloat>("FloatVal")!.Value);
        Assert.Equal(2.718281828459, readRoot.Get<NbtDouble>("DoubleVal")!.Value);
        Assert.Equal("Minecraft 1.21.4 Anvil Parity", readRoot.GetString("StringVal"));
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 255 }, readRoot.GetByteArray("ByteArrayVal"));
        Assert.Equal(new int[] { 100, 200, -300, 400 }, readRoot.GetIntArray("IntArrayVal"));
        Assert.Equal(new long[] { 1000L, -2000L, 3000L }, readRoot.GetLongArray("LongArrayVal"));

        var readList = readRoot.GetList("ListVal");
        Assert.NotNull(readList);
        Assert.Equal(2, readList.Count);
        Assert.Equal("Entry1", ((NbtString)readList[0]).Value);
        Assert.Equal("Entry2", ((NbtString)readList[1]).Value);

        var readInner = readRoot.GetCompound("InnerComp");
        Assert.NotNull(readInner);
        Assert.Equal(999, readInner.GetInt("InnerInt"));
    }
}
