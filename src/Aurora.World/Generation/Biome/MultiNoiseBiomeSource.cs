using System;
using System.Collections.Generic;

namespace Aurora.World.Generation.Biome;

/// <summary>
/// A BiomeSource that uses MultiNoise spatial mapping.
/// Biome climate parameters and sampling logic are ported and adapted from Pumpkin-MC and vanilla Minecraft.
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
            // 1. Deep Ocean (Continentalness < -0.45)
            new(new ParameterPoint(
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.5, -0.45), // Deep abyss
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                0.0), BiomeType.DeepOcean),

            // 2. Shallow Ocean (Continentalness -0.45 to -0.15)
            new(new ParameterPoint(
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                new Parameter(-0.45, -0.15), // Shelf / shallow ocean
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                0.0), BiomeType.Ocean),

            // 3. Beach (Continentalness -0.15 to -0.02)
            new(new ParameterPoint(
                new Parameter(-0.3, 0.8),
                new Parameter(-0.5, 0.8),
                new Parameter(-0.15, -0.02), // Coastline
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                0.0), BiomeType.Beach),

            // 4. Desert (Hot & Hyper-Arid inland)
            new(new ParameterPoint(
                new Parameter(0.5, 1.5),
                new Parameter(-1.5, -0.65),
                new Parameter(-0.02, 1.5),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 0.2),
                0.0), BiomeType.Desert),

            // 5. Jagged Peaks (High inland peaks, low erosion, high weirdness)
            new(new ParameterPoint(
                new Parameter(-1.5, 0.3),
                new Parameter(-1.0, 1.0),
                new Parameter(0.1, 1.5),
                new Parameter(-1.5, -0.4), // Steep jagged erosion
                new Parameter(-1.0, 1.0),
                new Parameter(0.3, 1.5),  // Ridge
                0.0), BiomeType.JaggedPeaks),

            // 6. Snowy Slopes (Cold mountains)
            new(new ParameterPoint(
                new Parameter(-1.5, -0.4),
                new Parameter(-1.0, 1.0),
                new Parameter(0.05, 1.5),
                new Parameter(-0.4, 0.3),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                0.0), BiomeType.SnowySlopes),

            // 7. Taiga (Cool & Humid coniferous forest)
            new(new ParameterPoint(
                new Parameter(-0.5, -0.05),
                new Parameter(0.05, 1.0),
                new Parameter(-0.02, 1.5),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                0.0), BiomeType.Taiga),

            // 8. Birch Forest (Temperate, moderate humidity, specific weirdness)
            new(new ParameterPoint(
                new Parameter(0.0, 0.45),
                new Parameter(0.1, 0.45),
                new Parameter(-0.02, 1.5),
                new Parameter(-0.5, 0.5),
                new Parameter(-1.0, 1.0),
                new Parameter(0.2, 1.0),
                0.0), BiomeType.BirchForest),

            // 9. Forest (Lush temperate wooded land)
            new(new ParameterPoint(
                new Parameter(-0.15, 0.4),
                new Parameter(0.15, 0.5),
                new Parameter(-0.02, 1.5),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                0.0), BiomeType.Forest),

            // 10. Plains (Balanced temperate open rolling fields)
            new(new ParameterPoint(
                new Parameter(-0.2, 0.4),
                new Parameter(-0.35, 0.2),
                new Parameter(-0.02, 1.5),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                0.0), BiomeType.Plains),

            // 11. Swamp (Warm, high humidity, flat lowland)
            new(new ParameterPoint(
                new Parameter(0.1, 0.55),
                new Parameter(0.5, 1.2),
                new Parameter(-0.08, 0.25),
                new Parameter(0.2, 0.8),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                0.0), BiomeType.Swamp),

            // 12. Savanna (Hot, dry grassland)
            new(new ParameterPoint(
                new Parameter(0.5, 1.1),
                new Parameter(-0.65, -0.15),
                new Parameter(0.0, 1.0),
                new Parameter(-0.5, 0.5),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                0.0), BiomeType.Savanna),

            // 13. Jungle (Hot, ultra-humid dense rainforest)
            new(new ParameterPoint(
                new Parameter(0.6, 1.3),
                new Parameter(0.6, 1.4),
                new Parameter(0.0, 1.0),
                new Parameter(-0.6, 0.4),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, 1.0),
                0.0), BiomeType.Jungle),

            // 14. Dark Forest (Temperate, dense humid dark canopy)
            new(new ParameterPoint(
                new Parameter(0.0, 0.35),
                new Parameter(0.5, 1.1),
                new Parameter(0.05, 0.8),
                new Parameter(-0.5, 0.5),
                new Parameter(-1.0, 1.0),
                new Parameter(-1.0, -0.2),
                0.0), BiomeType.DarkForest),

            // 15. Badlands (Ultra-hot, arid terracotta canyons)
            new(new ParameterPoint(
                new Parameter(0.8, 1.5),
                new Parameter(-1.5, -0.6),
                new Parameter(0.1, 1.2),
                new Parameter(0.1, 0.7),
                new Parameter(-1.0, 1.0),
                new Parameter(0.2, 1.5),
                0.0), BiomeType.Badlands)
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
