using System;
using System.Collections.Concurrent;
using Aurora.Core.Math;
using Aurora.World.Generation;

namespace Aurora.World;

/// <summary>
/// Manages active chunks and terrain generation in memory.
/// </summary>
public sealed class WorldManager
{
    private readonly ConcurrentDictionary<ChunkPosition, Chunk> _activeChunks = new();
    private readonly NoiseChunkGenerator _generator;

    public WorldManager(int seed = 12345)
    {
        _generator = new NoiseChunkGenerator(seed);
    }

    public Chunk GetOrGenerateChunk(int cx, int cz)
    {
        var pos = new ChunkPosition(cx, cz);
        return _activeChunks.GetOrAdd(pos, p => 
        {
            var chunk = _generator.GenerateChunk(p.X, p.Z);
            Aurora.World.Lighting.LightEngine.InitializeSkyLight(chunk);
            return chunk;
        });
    }

    public Chunk? GetChunk(int cx, int cz)
    {
        var pos = new ChunkPosition(cx, cz);
        _activeChunks.TryGetValue(pos, out var chunk);
        return chunk;
    }

    public void SetBlock(int x, int y, int z, ushort stateId)
    {
        int cx = x >> 4;
        int cz = z >> 4;
        var chunk = GetOrGenerateChunk(cx, cz);
        
        // chunk uses local coordinates (0-15) which are handled correctly internally by x & 15
        chunk.SetBlockState(x, y, z, stateId);
    }

    public ushort GetBlock(int x, int y, int z)
    {
        int cx = x >> 4;
        int cz = z >> 4;
        var chunk = GetChunk(cx, cz);
        
        if (chunk == null)
            return 0; // Air if chunk not loaded
            
        return chunk.GetBlockState(x, y, z);
    }
}
