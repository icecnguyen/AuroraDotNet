using System;
using System.Collections.Concurrent;
using Aurora.Core.Math;

namespace Aurora.World;

/// <summary>
/// Represents a physical dimension (Overworld, Nether, End).
/// </summary>
public sealed class Dimension
{
    public string Name { get; }
    private readonly ConcurrentDictionary<long, Chunk> _chunks = new();
    
    public Dimension(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    public void SetChunk(Chunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        _chunks[chunk.Position.LongHash] = chunk;
    }

    public Chunk? GetChunk(int x, int z)
    {
        var pos = new ChunkPosition(x, z);
        _chunks.TryGetValue(pos.LongHash, out var chunk);
        return chunk;
    }

    public void SetBlockState(int x, int y, int z, ushort stateId)
    {
        int chunkX = x >> 4;
        int chunkZ = z >> 4;
        var chunk = GetChunk(chunkX, chunkZ);
        if (chunk != null)
        {
            chunk.SetBlockState(x, y, z, stateId);
        }
    }

    public ushort GetBlockState(int x, int y, int z)
    {
        int chunkX = x >> 4;
        int chunkZ = z >> 4;
        var chunk = GetChunk(chunkX, chunkZ);
        if (chunk != null)
        {
            return chunk.GetBlockState(x, y, z);
        }
        return Block.Air;
    }
}
