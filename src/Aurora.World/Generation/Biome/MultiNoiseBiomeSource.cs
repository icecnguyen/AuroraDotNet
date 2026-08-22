using System;
using System.Collections.Generic;

namespace Aurora.World.Generation.Biome;

/// <summary>
/// A BiomeSource that uses MultiNoise spatial mapping.
/// </summary>
public sealed class MultiNoiseBiomeSource
{
    private readonly IReadOnlyList<KeyValuePair<ParameterPoint, BiomeType>> _biomeMappings;

    public MultiNoiseBiomeSource(IReadOnlyList<KeyValuePair<ParameterPoint, BiomeType>> biomeMappings)
    {
        ArgumentNullException.ThrowIfNull(biomeMappings);
        _biomeMappings = biomeMappings;
    }

    public static MultiNoiseBiomeSource CreateOverworld()
    {
        var mappings = new List<KeyValuePair<ParameterPoint, BiomeType>>
        {
            // Ocean: High moisture, low continentalness
            new(new ParameterPoint(
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, -0.2), // Deep water
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                0.0), BiomeType.Ocean),
                
            // Plains: Default fallback
            new(new ParameterPoint(
                new Parameter(-0.5, 0.5),
                new Parameter(-0.5, 0.5),
                new Parameter(-0.2, 1.0), // Land
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                0.0), BiomeType.Plains),
                
            // Desert: Hot, dry
            new(new ParameterPoint(
                new Parameter(0.5, 1.0),
                new Parameter(-1.0, -0.5),
                new Parameter(-0.2, 1.0),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                0.0), BiomeType.Desert),
                
            // Mountains: High weirdness or specific erosion/continentalness
            new(new ParameterPoint(
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                new Parameter(-0.2, 1.0),
                new Parameter(-1.0, -0.5), // High peaks
                new Parameter(-1.0, 1.0),
                new Parameter(0.5, 1.0), // Weirdness -> Peaks
                0.0), BiomeType.Mountains)
        };
        return new MultiNoiseBiomeSource(mappings);
    }

    /// <summary>
    /// Finds the closest matching biome for a given climate target point.
    /// In Minecraft, this uses a KD-Tree for optimization. 
    /// For now, a brute force distance check works for smaller biome counts.
    /// </summary>
    public BiomeType GetBiome(TargetPoint target)
    {
        BiomeType closestBiome = BiomeType.Plains;
        double minDistance = double.PositiveInfinity;

        foreach (var mapping in _biomeMappings)
        {
            double distance = mapping.Key.DistanceTo(target);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestBiome = mapping.Value;
            }
        }

        return closestBiome;
    }
}
