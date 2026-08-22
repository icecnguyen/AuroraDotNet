namespace Aurora.World.Generation.Density;

/// <summary>
/// A function that calculates a density value for a specific 3D coordinate.
/// Density Functions are the backbone of Minecraft 1.18+ terrain generation.
/// Positive density = solid block (stone), negative density = air/water.
/// </summary>
public interface IDensityFunction
{
    double Compute(NoiseContext context);
    
    // Limits for optimization
    double MinValue { get; }
    double MaxValue { get; }
}
