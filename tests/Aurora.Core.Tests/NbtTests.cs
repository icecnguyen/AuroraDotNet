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
}
