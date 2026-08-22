#pragma warning disable CA5394 // Random is insecure

using System;

namespace Aurora.World.Generation.Features;

public sealed class TreeConfiguration
{
    public ushort LogState { get; }
    public ushort LeavesState { get; }

    public TreeConfiguration(ushort logState, ushort leavesState)
    {
        LogState = logState;
        LeavesState = leavesState;
    }
}

public sealed class TreeFeature : IFeature
{
    private readonly TreeConfiguration _config;

    public TreeFeature(TreeConfiguration config)
    {
        _config = config;
    }

    public bool Place(FeatureContext context)
    {
        int x = context.OriginX;
        int y = context.OriginY;
        int z = context.OriginZ;
        
        // Basic tree height
        int height = 5 + context.Random.Next(3);
        
        // Check bounds
        if (y < -64 || y + height + 1 > 319) return false;

        // Ensure we are placing on dirt/grass. In a real system, the tree feature
        // itself doesn't check this, the PlacedFeature uses a BlockPredicateFilter.
        // For simplicity, we check here for now.
        ushort ground = context.Chunk.GetBlockState(x, y - 1, z);
        if (ground != SurfaceBuilder.GrassBlock && ground != SurfaceBuilder.Dirt)
        {
            return false;
        }

        // Leaves
        for (int ly = y + height - 3; ly <= y + height; ly++)
        {
            int rad = ly - (y + height);
            int radius = 1 - rad / 2;

            for (int lx = x - radius; lx <= x + radius; lx++)
            {
                if (lx < 0 || lx > 15) continue;
                for (int lz = z - radius; lz <= z + radius; lz++)
                {
                    if (lz < 0 || lz > 15) continue;
                    
                    if (Math.Abs(lx - x) == radius && Math.Abs(lz - z) == radius && (context.Random.Next(2) == 0 || rad == 0))
                    {
                        continue;
                    }
                    
                    if (context.Chunk.GetBlockState(lx, ly, lz) == 0) // Only replace air
                    {
                        context.Chunk.SetBlockState(lx, ly, lz, _config.LeavesState);
                    }
                }
            }
        }

        // Trunk
        for (int ty = 0; ty < height; ty++)
        {
            context.Chunk.SetBlockState(x, y + ty, z, _config.LogState);
        }

        return true;
    }
}
