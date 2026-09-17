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

    [Fact]
    public void NoiseChunkGeneratorGeneratesNaturalVanillaTerrain()
    {
        var generator = new Generation.NoiseChunkGenerator(12345);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var chunk = generator.GenerateChunk(0, 0);
        stopwatch.Stop();

        // Ensure generation is fast (under 100ms on test runner, typical is <5ms)
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Chunk generation took {stopwatch.ElapsedMilliseconds}ms, which is too slow.");

        // Bedrock at Y = -64
        Assert.Equal(Block.Bedrock, chunk.GetBlockState(0, -64, 0));

        // High in the sky is Air
        Assert.Equal(Block.Air, chunk.GetBlockState(0, 310, 0));

        // Deep underground (Y = -30) should be Deepslate or Ore
        ushort deepBlock = chunk.GetBlockState(0, -30, 0);
        Assert.True(deepBlock == Block.Deepslate || deepBlock == Block.Stone || deepBlock > 100,
            $"Expected deep block to be Deepslate or Ore, but got {deepBlock}");
    }

    [Fact]
    public void FindSpawnPositionFindsSpawnQuickly()
    {
        var wm = new WorldManager(12345);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var spawn = wm.FindSpawnPosition();
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 500, $"FindSpawn took {sw.ElapsedMilliseconds}ms at ({spawn.X}, {spawn.Y}, {spawn.Z})");
    }
}
