using System;
using System.IO;
using Aurora.Core.Math;
using Aurora.Core.Nbt;
using Aurora.World.Storage;
using Xunit;

namespace Aurora.World.Tests;

public class AnvilNbtTests
{
    [Fact]
    public void ChunkNbtSerializerProducesStandardMinecraftCompound()
    {
        var pos = new ChunkPosition(3, -7);
        var chunk = new Chunk(pos);

        // Populate various sections
        chunk.SetBlockState(0, -64, 0, Block.Bedrock);
        chunk.SetBlockState(5, -30, 5, Block.DeepslateDiamondOre);
        chunk.SetBlockState(10, 15, 10, Block.Stone);
        chunk.SetBlockState(8, 70, 8, Block.GrassBlock);
        chunk.SetBlockState(8, 69, 8, Block.Dirt);
        chunk.SetBlockState(4, 50, 4, Block.Water);
        chunk.SetBlockState(2, -58, 2, Block.Lava);

        chunk.SetSkyLight(8, 70, 8, 15);
        chunk.SetBlockLight(2, -58, 2, 14);

        // Serialize to NBT
        var root = ChunkNbtSerializer.Serialize(chunk);

        // Assert root metadata
        Assert.Equal(4082, root.GetInt("DataVersion"));
        Assert.Equal(3, root.GetInt("xPos"));
        Assert.Equal(-7, root.GetInt("zPos"));
        Assert.Equal(-4, root.GetInt("yPos"));
        Assert.Equal("minecraft:full", root.GetString("Status"));

        // Assert sections list
        var sections = root.GetList("sections");
        Assert.NotNull(sections);
        Assert.Equal(24, sections.Count);

        // Test roundtrip deserialization
        var deserialized = ChunkNbtSerializer.Deserialize(root);
        Assert.Equal(pos.X, deserialized.Position.X);
        Assert.Equal(pos.Z, deserialized.Position.Z);

        Assert.Equal(Block.Bedrock, deserialized.GetBlockState(0, -64, 0));
        Assert.Equal(Block.DeepslateDiamondOre, deserialized.GetBlockState(5, -30, 5));
        Assert.Equal(Block.Stone, deserialized.GetBlockState(10, 15, 10));
        Assert.Equal(Block.GrassBlock, deserialized.GetBlockState(8, 70, 8));
        Assert.Equal(Block.Dirt, deserialized.GetBlockState(8, 69, 8));
        Assert.Equal(Block.Water, deserialized.GetBlockState(4, 50, 4));
        Assert.Equal(Block.Lava, deserialized.GetBlockState(2, -58, 2));

        Assert.Equal(15, deserialized.GetSkyLight(8, 70, 8));
        Assert.Equal(14, deserialized.GetBlockLight(2, -58, 2));
    }

    [Fact]
    public void AnvilStorageNbtRoundtripThroughRegionFile()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "aurora_nbt_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var chunkPos = new ChunkPosition(12, -4);
            var chunk = new Chunk(chunkPos);

            chunk.SetBlockState(0, -64, 0, Block.Bedrock);
            chunk.SetBlockState(7, 65, 7, Block.GoldBlock);
            chunk.SetBlockState(7, 64, 7, Block.OakPlanks);
            chunk.SetSkyLight(7, 65, 7, 13);
            chunk.SetBlockLight(7, 64, 7, 7);

            // 1. Save chunk using AnvilWorldStorage
            using (var storage = new AnvilWorldStorage(tempDir))
            {
                storage.SaveChunk(chunk);
            }

            // 2. Read raw region file sector to verify it's standard NBT, not custom AURO
            string regionPath = Path.Combine(tempDir, "region", "r.0.-1.mca");
            Assert.True(File.Exists(regionPath));

            using (var region = new RegionFile(regionPath))
            {
                byte[]? rawData = region.ReadChunkData(chunkPos.X, chunkPos.Z);
                Assert.NotNull(rawData);

                // Deserialize raw stream with NbtReader directly
                using var ms = new MemoryStream(rawData);
                var root = NbtReader.ReadRoot(ms);
                Assert.Equal(4082, root.GetInt("DataVersion"));
                Assert.Equal("minecraft:full", root.GetString("Status"));
            }

            // 3. Load chunk back using AnvilWorldStorage
            using (var storage = new AnvilWorldStorage(tempDir))
            {
                bool loaded = storage.TryLoadChunk(chunkPos, out var loadedChunk);
                Assert.True(loaded);
                Assert.NotNull(loadedChunk);

                Assert.Equal(Block.Bedrock, loadedChunk.GetBlockState(0, -64, 0));
                Assert.Equal(Block.GoldBlock, loadedChunk.GetBlockState(7, 65, 7));
                Assert.Equal(Block.OakPlanks, loadedChunk.GetBlockState(7, 64, 7));
                Assert.Equal(13, loadedChunk.GetSkyLight(7, 65, 7));
                Assert.Equal(7, loadedChunk.GetBlockLight(7, 64, 7));
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
