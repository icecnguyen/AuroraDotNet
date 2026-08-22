using System;
using System.Collections.Generic;
using Aurora.Core.Math;

namespace Aurora.World.Lighting;

public sealed class LightEngine
{
    private readonly WorldManager _worldManager;

    public LightEngine(WorldManager worldManager)
    {
        _worldManager = worldManager;
    }

    /// <summary>
    /// Initializes Sky Light for a newly generated chunk.
    /// Propagates maximum light (15) straight down until it hits an opaque block.
    /// </summary>
    public static void InitializeSkyLight(Chunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        
        // Simple downward propagation for SkyLight
        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                byte currentLight = 15;
                for (int y = 319; y >= -64; y--)
                {
                    ushort block = chunk.GetBlockState(x, y, z);
                    
                    // If block is not air and not water, it blocks light (simplified)
                    // TODO: Use proper Block Registry to check opacity
                    if (block != Aurora.World.Generation.SurfaceBuilder.Air && 
                        block != Aurora.World.Generation.SurfaceBuilder.Water)
                    {
                        currentLight = 0;
                    }
                    else if (block == Aurora.World.Generation.SurfaceBuilder.Water)
                    {
                        if (currentLight > 2) currentLight -= 2;
                        else currentLight = 0;
                    }

                    chunk.SetSkyLight(x, y, z, currentLight);
                    chunk.SetBlockLight(x, y, z, 0);
                }
            }
        }
    }

    /// <summary>
    /// Recalculates block light originating from a specific block update.
    /// </summary>
    public static void UpdateBlockLight(int x, int y, int z, int newLightLevel)
    {
        // To be implemented: flood fill BFS
    }
    
    private readonly struct LightNode
    {
        public readonly Coordinate Position;
        public readonly byte LightLevel;

        public LightNode(Coordinate position, byte lightLevel)
        {
            Position = position;
            LightLevel = lightLevel;
        }
    }
}
