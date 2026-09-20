using System;
using System.Collections.Generic;
using Aurora.Core.Math.Noise;
using Aurora.Core.Math.Random;

namespace Aurora.World.Generation.Aquifers;

/// <summary>
/// Samples hydrostatic water tables and subterranean aquifer reservoirs ported from Pumpkin-MC and Vanilla 1.18+.
/// Dictates whether carved cave air is filled with water, lava, or empty air.
/// </summary>
public sealed class AquiferSampler
{
    private const int GlobalSeaLevel = 62;
    private const int LavaSeaLevel = -54;

    private readonly OctavePerlinNoise _fluidLevelNoise;
    private readonly OctavePerlinNoise _aquiferBarrierNoise;

    public AquiferSampler(long seed)
    {
        var randomFactory = new PositionalRandomFactory(seed);

        // Low-frequency noise for varying underground water tables
        _fluidLevelNoise = new OctavePerlinNoise(
            randomFactory.FromHashOf("aquifer_water_level").At(0, 0, 0),
            -2,
            new List<double> { 1.0, 1.0 });

        // 3D noise for distinct subterranean flooded aquifer pockets
        _aquiferBarrierNoise = new OctavePerlinNoise(
            randomFactory.FromHashOf("aquifer_barrier").At(0, 0, 0),
            -3,
            new List<double> { 1.0, 1.0, 1.0 });
    }

    /// <summary>
    /// Evaluates the fluid to fill when a carver excavates a block at (worldX, worldY, worldZ).
    /// </summary>
    public ushort GetCarveFluid(int worldX, int worldY, int worldZ)
    {
        // 1. Deepest subterranean layer: always filled with bedrock lava lakes
        if (worldY <= LavaSeaLevel)
        {
            return Block.Lava;
        }

        // 2. Underwater ravines/open caves intersecting sea level (oceans, swamps, rivers)
        if (worldY <= GlobalSeaLevel)
        {
            // Sample local aquifer noise to create natural flooded underground caves
            double barrier = _aquiferBarrierNoise.GetValue(worldX * 0.025, worldY * 0.035, worldZ * 0.025);
            
            // If inside a flooded cavern pocket (approx 20% of underground caves)
            if (barrier > 0.30)
            {
                double levelVariation = _fluidLevelNoise.GetValue(worldX * 0.015, 0, worldZ * 0.015) * 16.0;
                int localWaterTable = Math.Clamp(GlobalSeaLevel - 15 + (int)Math.Round(levelVariation), -30, GlobalSeaLevel);

                if (worldY <= localWaterTable)
                {
                    return Block.Water;
                }
            }
        }

        return Block.Air;
    }
}
