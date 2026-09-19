using System;
using System.Collections.Concurrent;
using Aurora.Core.Math;
using Aurora.World.Generation;
using Aurora.World.Storage;

namespace Aurora.World;

/// <summary>
/// Manages active chunks, terrain generation, and Anvil (.mca) persistence in memory.
/// </summary>
public sealed class WorldManager : IDisposable
{
    private readonly ConcurrentDictionary<ChunkPosition, Chunk> _activeChunks = new();
    private readonly NoiseChunkGenerator _generator;
    private readonly AnvilWorldStorage _storage;

    public WorldManager(int seed = 12345, string worldDirectory = "world")
    {
        _generator = new NoiseChunkGenerator(seed);
        _storage = new AnvilWorldStorage(worldDirectory);
    }

    public Chunk GetOrGenerateChunk(int cx, int cz)
    {
        var pos = new ChunkPosition(cx, cz);
        return _activeChunks.GetOrAdd(pos, p => 
        {
            // 1. Try loading existing chunk from Anvil region file (.mca)
            if (_storage.TryLoadChunk(p, out var savedChunk) && savedChunk != null)
            {
                return savedChunk;
            }

            // 2. Procedural terrain generation
            var chunk = _generator.GenerateChunk(p.X, p.Z);
            Aurora.World.Lighting.LightEngine.InitializeLighting(chunk);

            // 3. Persist new chunk to disk
            _storage.SaveChunk(chunk);
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
        _storage.SaveChunk(chunk);
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

    private (double X, double Y, double Z)? _cachedSpawnPosition;

    /// <summary>
    /// Searches outwards from (0, 0) for a solid, land-based spawn position above sea level (Y >= 64).
    /// </summary>
    public (double X, double Y, double Z) FindSpawnPosition()
    {
        if (_cachedSpawnPosition.HasValue)
            return _cachedSpawnPosition.Value;

        for (int radius = 0; radius <= 8; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dz)) != radius)
                        continue;

                    var chunk = GetOrGenerateChunk(dx, dz);
                    for (int y = 140; y >= 64; y--)
                    {
                        ushort block = chunk.GetBlockState(8, y, 8);
                        if (block == Block.GrassBlock || block == Block.Sand || block == Block.Podzol || block == Block.Stone || block == Block.SnowBlock)
                        {
                            if (chunk.GetBlockState(8, y + 1, 8) == Block.Air && chunk.GetBlockState(8, y + 2, 8) == Block.Air)
                            {
                                double worldX = dx * 16 + 8.5;
                                double worldY = y + 1.0;
                                double worldZ = dz * 16 + 8.5;
                                _cachedSpawnPosition = (worldX, worldY, worldZ);
                                return _cachedSpawnPosition.Value;
                            }
                        }
                    }
                }
            }
        }

        _cachedSpawnPosition = (0.5, 75.0, 0.5);
        return _cachedSpawnPosition.Value;
    }

    private readonly ConcurrentDictionary<int, Entities.ItemEntity> _activeItemEntities = new();

    public IEnumerable<Entities.ItemEntity> ItemEntities => _activeItemEntities.Values;

    public void AddItemEntity(Entities.ItemEntity itemEntity)
    {
        ArgumentNullException.ThrowIfNull(itemEntity);
        _activeItemEntities.TryAdd(itemEntity.EntityId, itemEntity);
    }

    public bool RemoveItemEntity(int entityId)
    {
        return _activeItemEntities.TryRemove(entityId, out _);
    }

    public void Tick()
    {
        foreach (var item in _activeItemEntities.Values)
        {
            item.Tick(this);
            if (item.IsDead)
            {
                _activeItemEntities.TryRemove(item.EntityId, out _);
            }
        }
    }

    public void Dispose()
    {
        _storage.Dispose();
    }
}
