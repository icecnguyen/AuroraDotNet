using Aurora.Core.Math;
using Xunit;

namespace Aurora.World.Tests;

public class WorldTests
{
    [Fact]
    public void FlatWorldGeneratorCreatesCorrectTerrain()
    {
        var chunk = FlatWorldGenerator.GenerateChunk(0, 0);

        Assert.Equal(Block.Bedrock, chunk.GetBlockState(0, -64, 0));
        Assert.Equal(Block.Dirt, chunk.GetBlockState(0, -63, 0));
        Assert.Equal(Block.GrassBlock, chunk.GetBlockState(0, -61, 0));
        Assert.Equal(Block.Air, chunk.GetBlockState(0, 0, 0));
    }

    [Fact]
    public void DimensionManagesChunksAndBlocks()
    {
        var dimension = new Dimension("Overworld");
        var chunk = FlatWorldGenerator.GenerateChunk(1, -1);
        
        dimension.SetChunk(chunk);
        
        // Chunk is at 1, -1 => block coordinates X: 16 to 31, Z: -16 to -1
        Assert.Equal(Block.Bedrock, dimension.GetBlockState(16, -64, -16));
        
        // Set block across dimension
        dimension.SetBlockState(16, 10, -16, Block.Stone);
        
        Assert.Equal(Block.Stone, chunk.GetBlockState(0, 10, 0)); // local 0,0 corresponds to 16, -16
        Assert.Equal(Block.Stone, dimension.GetBlockState(16, 10, -16));
    }
}
