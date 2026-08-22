#pragma warning disable CA5394 // Random is insecure

using System;

namespace Aurora.World.Generation.Features;

public interface IFeature
{
    bool Place(FeatureContext context);
}

public readonly record struct FeatureContext(
    Chunk Chunk,
    int OriginX,
    int OriginY,
    int OriginZ,
    Random Random
);

public sealed class OreConfiguration
{
    public ushort TargetState { get; }
    public ushort State { get; }
    public int Size { get; }

    public OreConfiguration(ushort targetState, ushort state, int size)
    {
        TargetState = targetState;
        State = state;
        Size = size;
    }
}

public sealed class OreFeature : IFeature
{
    private readonly OreConfiguration _config;

    public OreFeature(OreConfiguration config)
    {
        _config = config;
    }

    public bool Place(FeatureContext context)
    {
        // Simple blob generation algorithm for ores.
        // Minecraft 1.18 uses a much more complex math-based approach for veins, 
        // but for block blobs, it uses math to draw intersecting ellipsoids.
        
        float angle = (float)(context.Random.NextDouble() * Math.PI);
        int size = _config.Size;
        
        float x1 = context.OriginX + (float)Math.Sin(angle) * size / 8.0f;
        float x2 = context.OriginX - (float)Math.Sin(angle) * size / 8.0f;
        float z1 = context.OriginZ + (float)Math.Cos(angle) * size / 8.0f;
        float z2 = context.OriginZ - (float)Math.Cos(angle) * size / 8.0f;
        
        float y1 = context.OriginY + context.Random.Next(-2, 3);
        float y2 = context.OriginY + context.Random.Next(-2, 3);
        
        for (int i = 0; i < size; i++)
        {
            float t = (float)i / size;
            float centerX = x1 + (x2 - x1) * t;
            float centerY = y1 + (y2 - y1) * t;
            float centerZ = z1 + (z2 - z1) * t;
            
            float radiusMultiplier = (float)(context.Random.NextDouble() * size / 16.0);
            float radius = (float)(Math.Sin(Math.PI * t) + 1.0f) * radiusMultiplier + 0.5f;
            
            int minX = (int)(centerX - radius);
            int minY = (int)(centerY - radius);
            int minZ = (int)(centerZ - radius);
            
            int maxX = (int)(centerX + radius);
            int maxY = (int)(centerY + radius);
            int maxZ = (int)(centerZ + radius);
            
            for (int bx = minX; bx <= maxX; bx++)
            {
                if (bx < 0 || bx > 15) continue;
                double dx = Math.Pow((bx + 0.5 - centerX) / radius, 2);
                if (dx >= 1.0) continue;
                
                for (int by = minY; by <= maxY; by++)
                {
                    if (by < -64 || by > 319) continue;
                    double dy = Math.Pow((by + 0.5 - centerY) / radius, 2);
                    if (dx + dy >= 1.0) continue;
                    
                    for (int bz = minZ; bz <= maxZ; bz++)
                    {
                        if (bz < 0 || bz > 15) continue;
                        double dz = Math.Pow((bz + 0.5 - centerZ) / radius, 2);
                        
                        if (dx + dy + dz < 1.0)
                        {
                            if (context.Chunk.GetBlockState(bx, by, bz) == _config.TargetState)
                            {
                                context.Chunk.SetBlockState(bx, by, bz, _config.State);
                            }
                        }
                    }
                }
            }
        }
        
        return true;
    }
}
