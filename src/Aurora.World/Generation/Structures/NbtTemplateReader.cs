using System;
using System.Collections.Generic;

namespace Aurora.World.Generation.Structures;

/// <summary>
/// Mocks the reading of Minecraft .nbt structure files.
/// A full NBT parser requires an entire NBT library (e.g. fNbt) which we skip for this phase.
/// </summary>
public static class NbtTemplateReader
{
    public static StructureTemplate Load(string resourceLocation)
    {
        // Mock a 5x5x5 simple house
        int sizeX = 5;
        int sizeY = 5;
        int sizeZ = 5;
        ushort[] blocks = new ushort[sizeX * sizeY * sizeZ];
        
        // Fill with air
        for (int i = 0; i < blocks.Length; i++) blocks[i] = 0;
        
        // Build walls (using Oak Planks, default state ID = 15)
        ushort planks = 15;
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    if (x == 0 || x == sizeX - 1 || z == 0 || z == sizeZ - 1 || y == 0 || y == sizeY - 1)
                    {
                        blocks[y * (sizeX * sizeZ) + z * sizeX + x] = planks;
                    }
                }
            }
        }
        
        // Add a jigsaw block (connection point) in the middle of one wall
        var jigsawBlocks = new List<JigsawBlock>
        {
            new JigsawBlock(2, 1, 0, "minecraft:building_entrance", "minecraft:street", "minecraft:village/plains/streets", "aligned")
        };
        
        return new StructureTemplate(sizeX, sizeY, sizeZ, blocks, jigsawBlocks);
    }
}
