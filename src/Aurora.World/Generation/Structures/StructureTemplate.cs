using System;
using System.Collections.Generic;

namespace Aurora.World.Generation.Structures;

/// <summary>
/// Represents a pre-built structure (loaded from an NBT file or generated in memory).
/// </summary>
public sealed class StructureTemplate
{
    public int SizeX { get; }
    public int SizeY { get; }
    public int SizeZ { get; }
    
    private readonly ushort[] _blocks;
    public IReadOnlyList<JigsawBlock> JigsawBlocks { get; }

    public StructureTemplate(int sizeX, int sizeY, int sizeZ, ushort[] blocks, IReadOnlyList<JigsawBlock> jigsawBlocks)
    {
        SizeX = sizeX;
        SizeY = sizeY;
        SizeZ = sizeZ;
        _blocks = blocks;
        JigsawBlocks = jigsawBlocks;
    }

    public ushort GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= SizeX || y < 0 || y >= SizeY || z < 0 || z >= SizeZ)
            return 0; // Air
            
        return _blocks[y * (SizeX * SizeZ) + z * SizeX + x];
    }
}
