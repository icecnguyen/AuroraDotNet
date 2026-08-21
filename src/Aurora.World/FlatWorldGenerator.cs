using Aurora.Core.Math;

namespace Aurora.World;

/// <summary>
/// Generates completely flat terrain chunks.
/// </summary>
public static class FlatWorldGenerator
{
    public static Chunk GenerateChunk(int chunkX, int chunkZ)
    {
        var chunk = new Chunk(new ChunkPosition(chunkX, chunkZ));

        // Generate standard superflat: 1 bedrock, 2 dirt, 1 grass
        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                chunk.SetBlockState(x, -64, z, Block.Bedrock);
                chunk.SetBlockState(x, -63, z, Block.Dirt);
                chunk.SetBlockState(x, -62, z, Block.Dirt);
                chunk.SetBlockState(x, -61, z, Block.GrassBlock);
            }
        }

        return chunk;
    }
}
