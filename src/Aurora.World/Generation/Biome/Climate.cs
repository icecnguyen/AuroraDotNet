using System;
using System.Collections.Generic;
using Aurora.World.Generation.Density;

namespace Aurora.World.Generation.Biome;

/// <summary>
/// Represents the actual climate parameters evaluated at a specific x, y, z in the world.
/// </summary>
public readonly record struct TargetPoint(
    double Temperature,
    double Humidity,
    double Continentalness,
    double Erosion,
    double Depth,
    double Weirdness
);

/// <summary>
/// Represents a range [Min, Max] of a specific climate parameter that a Biome allows.
/// </summary>
public readonly record struct Parameter(double Min, double Max)
{
    public double Distance(double value)
    {
        if (value < Min) return Min - value;
        if (value > Max) return value - Max;
        return 0.0;
    }
}

/// <summary>
/// Represents the ideal climate constraints for a specific Biome.
/// </summary>
public readonly record struct ParameterPoint(
    Parameter Temperature,
    Parameter Humidity,
    Parameter Continentalness,
    Parameter Erosion,
    Parameter Depth,
    Parameter Weirdness,
    double Offset
)
{
    public double DistanceTo(TargetPoint target)
    {
        return Math.Pow(Temperature.Distance(target.Temperature), 2)
             + Math.Pow(Humidity.Distance(target.Humidity), 2)
             + Math.Pow(Continentalness.Distance(target.Continentalness), 2)
             + Math.Pow(Erosion.Distance(target.Erosion), 2)
             + Math.Pow(Depth.Distance(target.Depth), 2)
             + Math.Pow(Weirdness.Distance(target.Weirdness), 2)
             + Math.Pow(Offset, 2);
    }
}

public static class Climate
{
    /// <summary>
    /// Evaluates the target point at a given coordinate using the NoiseRouter.
    /// </summary>
    public static TargetPoint Target(NoiseRouter router, NoiseContext context)
    {
        ArgumentNullException.ThrowIfNull(router);
        
        return new TargetPoint(
            router.Temperature.Compute(context),
            router.Humidity.Compute(context),
            router.Continentalness.Compute(context),
            router.Erosion.Compute(context),
            router.Depth.Compute(context),
            router.Weirdness.Compute(context)
        );
    }
}
