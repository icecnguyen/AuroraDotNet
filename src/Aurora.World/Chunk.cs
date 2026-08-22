using System;
using Aurora.Core.Math;

namespace Aurora.World;

/// <summary>
/// Represents a 16x384x16 vertical chunk slice (Y: -64 to 319).
/// </summary>
public sealed class Chunk
{
    public ChunkPosition Position { get; }
    
    // Linear array for block state IDs (ushort), matching Phase 6 rules.
    // Length: 16 * 384 * 16 = 98304 elements.
    private readonly ushort[] _blocks = new ushort[16 * 384 * 16];

    // Light arrays (4-bit values stored as full bytes for performance in RAM)
    private readonly byte[] _skyLight = new byte[16 * 384 * 16];
    private readonly byte[] _blockLight = new byte[16 * 384 * 16];

    public Chunk(ChunkPosition position)
    {
        Position = position;
        Array.Fill(_skyLight, (byte)15); // Default to fully lit by sky
    }

    public void SetBlockState(int x, int y, int z, ushort stateId)
    {
        if (y is < -64 or >= 320) return;
        
        // Wrap local chunk coordinates (x and z must be 0-15)
        x &= 15;
        z &= 15;
        
        int index = GetIndex(x, y, z);
        _blocks[index] = stateId;
    }

    public ushort GetBlockState(int x, int y, int z)
    {
        if (y is < -64 or >= 320) return Block.Air;
        
        x &= 15;
        z &= 15;
        
        int index = GetIndex(x, y, z);
        return _blocks[index];
    }

    public void SetSkyLight(int x, int y, int z, byte lightLevel)
    {
        if (y is < -64 or >= 320) return;
        x &= 15; z &= 15;
        _skyLight[GetIndex(x, y, z)] = lightLevel;
    }

    public byte GetSkyLight(int x, int y, int z)
    {
        if (y is < -64 or >= 320) return 15;
        x &= 15; z &= 15;
        return _skyLight[GetIndex(x, y, z)];
    }

    public void SetBlockLight(int x, int y, int z, byte lightLevel)
    {
        if (y is < -64 or >= 320) return;
        x &= 15; z &= 15;
        _blockLight[GetIndex(x, y, z)] = lightLevel;
    }

    public byte GetBlockLight(int x, int y, int z)
    {
        if (y is < -64 or >= 320) return 0;
        x &= 15; z &= 15;
        return _blockLight[GetIndex(x, y, z)];
    }

    // Calculates the flat 1D index from 3D coordinates.
    // Order: Y, Z, X (Standard Minecraft Palette layout format for chunks)
    private static int GetIndex(int x, int y, int z)
    {
        int localY = y + 64;
        return (localY * 256) + (z * 16) + x;
    }
    
    public ReadOnlySpan<ushort> RawBlocks => _blocks;
    public ReadOnlySpan<byte> RawSkyLight => _skyLight;
    public ReadOnlySpan<byte> RawBlockLight => _blockLight;
}
