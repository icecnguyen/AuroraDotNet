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

        // Ensure generation is fast (typical is <15ms, 500ms allowance for JIT / high CPU parallel test runs)
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Chunk generation took {stopwatch.ElapsedMilliseconds}ms, which is too slow.");

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
        using var wm = new WorldManager(12345);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var spawn = wm.FindSpawnPosition();
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 1500, $"FindSpawn took {sw.ElapsedMilliseconds}ms at ({spawn.X}, {spawn.Y}, {spawn.Z})");
    }

    [Fact]
    public void AnvilRegionStorageSavesAndLoadsChunkAccurately()
    {
        string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "aurora_test_" + System.Guid.NewGuid().ToString("N"));
        try
        {
            var chunkPos = new ChunkPosition(2, 3);
            var originalChunk = new Chunk(chunkPos);
            originalChunk.SetBlockState(5, 70, 5, Block.DiamondOre);
            originalChunk.SetBlockState(0, -64, 0, Block.Bedrock);
            originalChunk.SetSkyLight(5, 70, 5, 12);
            originalChunk.SetBlockLight(5, 70, 5, 8);

            using (var storage = new Storage.AnvilWorldStorage(tempDir))
            {
                storage.SaveChunk(originalChunk);
            }

            using (var storage = new Storage.AnvilWorldStorage(tempDir))
            {
                bool loaded = storage.TryLoadChunk(chunkPos, out var loadedChunk);
                Assert.True(loaded);
                Assert.NotNull(loadedChunk);
                Assert.Equal(Block.DiamondOre, loadedChunk.GetBlockState(5, 70, 5));
                Assert.Equal(Block.Bedrock, loadedChunk.GetBlockState(0, -64, 0));
                Assert.Equal(12, loadedChunk.GetSkyLight(5, 70, 5));
                Assert.Equal(8, loadedChunk.GetBlockLight(5, 70, 5));
            }
        }
        finally
        {
            if (System.IO.Directory.Exists(tempDir))
            {
                System.IO.Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void ExpandedBiomesAreMappedInMultiNoiseSource()
    {
        var source = Generation.Biome.MultiNoiseBiomeSource.CreateOverworld();
        
        // Target points matching each expanded climate
        var swampTarget = new Generation.Biome.TargetPoint(0.3, 0.8, 0.1, 0.5, 0.0, 0.0);
        Assert.Equal(Generation.BiomeType.Swamp, source.GetBiome(swampTarget));

        var savannaTarget = new Generation.Biome.TargetPoint(0.8, -0.5, 0.5, 0.0, 0.0, 0.0);
        Assert.Equal(Generation.BiomeType.Savanna, source.GetBiome(savannaTarget));

        var jungleTarget = new Generation.Biome.TargetPoint(0.9, 0.9, 0.5, 0.0, 0.0, 0.0);
        Assert.Equal(Generation.BiomeType.Jungle, source.GetBiome(jungleTarget));

        var badlandsTarget = new Generation.Biome.TargetPoint(1.1, -1.0, 0.5, 0.3, 0.0, 0.5);
        Assert.Equal(Generation.BiomeType.Badlands, source.GetBiome(badlandsTarget));
    }

    [Fact]
    public void CarversNeverBreakBedrock()
    {
        var chunk = new Chunk(new ChunkPosition(0, 0));
        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                chunk.SetBlockState(x, -64, z, Block.Bedrock);
                for (int y = -63; y <= 80; y++)
                    chunk.SetBlockState(x, y, z, Block.Stone);
            }
        }

        var caveCarver = new Generation.Carvers.CaveCarver(12345);
        var canyonCarver = new Generation.Carvers.CanyonCarver(12345);

        caveCarver.Carve(chunk, 0, 0);
        canyonCarver.Carve(chunk, 0, 0);

        // Verify Bedrock is intact for all 256 columns
        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                Assert.Equal(Block.Bedrock, chunk.GetBlockState(x, -64, z));
            }
        }
    }
}
