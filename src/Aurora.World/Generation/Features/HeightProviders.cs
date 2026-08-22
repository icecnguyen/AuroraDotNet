#pragma warning disable CA5394 // Random is insecure

using System;
using Aurora.World.Generation.Density;

namespace Aurora.World.Generation.Features;

/// <summary>
/// Provides a Y-coordinate for feature placement based on specific distributions.
/// </summary>
public abstract class HeightProvider
{
    public abstract int Sample(Random random, NoiseContext context);
}

/// <summary>
/// A height provider that picks a Y-level uniformly between Min and Max.
/// </summary>
public sealed class UniformHeightProvider : HeightProvider
{
    public int MinY { get; }
    public int MaxY { get; }

    public UniformHeightProvider(int minY, int maxY)
    {
        MinY = minY;
        MaxY = maxY;
    }

    public override int Sample(Random random, NoiseContext context)
    {
        if (MinY >= MaxY) return MinY;
        return random.Next(MinY, MaxY + 1);
    }
}

/// <summary>
/// A height provider that biases Y-levels towards the center of the range.
/// Crucial for Minecraft 1.18+ ore distribution (e.g. Diamonds spawn more frequently at the bottom).
/// </summary>
public sealed class TrapezoidHeightProvider : HeightProvider
{
    public int MinY { get; }
    public int MaxY { get; }
    public int Plateau { get; }

    public TrapezoidHeightProvider(int minY, int maxY, int plateau = 0)
    {
        MinY = minY;
        MaxY = maxY;
        Plateau = plateau;
    }

    public override int Sample(Random random, NoiseContext context)
    {
        if (MinY >= MaxY) return MinY;
        
        int range = MaxY - MinY;
        int halfRange = (range - Plateau) / 2;
        
        int r1 = random.Next(0, range - halfRange + 1);
        int r2 = random.Next(0, halfRange + 1);
        
        return MinY + r1 + r2;
    }
}

/// <summary>
/// A height provider that always returns the highest solid block.
/// Very inefficient compared to a true Heightmap, but works for Phase 4.
/// </summary>
public sealed class SurfaceHeightProvider : HeightProvider
{
    public override int Sample(Random random, NoiseContext context)
    {
        // Actually, since we don't have the chunk here, this is hard to implement correctly 
        // without altering the HeightProvider signature to take the Chunk.
        // Let's return a dummy value and let a custom PlacedFeature logic handle it,
        // or just pass Chunk into Sample.
        throw new NotImplementedException("Use TopSolidFeature wrapper instead.");
    }
}
