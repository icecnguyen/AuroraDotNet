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

    private static void WriteVarInt(System.IO.MemoryStream ms, int value)
    {
        uint uval = (uint)value;
        while ((uval & ~0x7Fu) != 0)
        {
            ms.WriteByte((byte)((uval & 0x7F) | 0x80));
            uval >>= 7;
        }
        ms.WriteByte((byte)uval);
    }

    public static byte[] GenerateChunkData(Chunk chunk)
    {
        System.ArgumentNullException.ThrowIfNull(chunk);
        
        using var ms = new System.IO.MemoryStream();
        
        // 24 Sections (Y from -64 to 319)
        for (int i = 0; i < 24; i++)
        {
            int sectionY = -64 + (i * 16);
            ushort sectionBlock = chunk.GetBlockState(0, sectionY, 0);
            short blockCount = sectionBlock == 0 ? (short)0 : (short)4096;
            
            // Write BlockCount (Big Endian)
            ms.WriteByte((byte)(blockCount >> 8));
            ms.WriteByte((byte)(blockCount & 0xFF));
            
            // Write BlockStates Paletted Container
            ms.WriteByte(0); // Bits per entry = 0 (Single Valued)
            WriteVarInt(ms, sectionBlock); // The block state ID
            WriteVarInt(ms, 0); // Data array length = 0
            
            // Write Biomes Paletted Container
            ms.WriteByte(0); // Bits per entry = 0 (Single Valued)
            WriteVarInt(ms, 0); // Plains biome (0)
            WriteVarInt(ms, 0); // Data array length = 0
        }
        
        return ms.ToArray();
    }
}
