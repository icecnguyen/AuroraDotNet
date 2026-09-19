#pragma warning disable CA5394 // Random is insecure

using System;

namespace Aurora.World.Generation;

/// <summary>
/// Replaces the top surface blocks and sub-surface blocks of the generated terrain
/// with biome-specific blocks, water, deepslate, and bedrock.
/// </summary>
public sealed class SurfaceBuilder
{
    // Aliases to Block constants for backwards compatibility
    public const ushort Air = Block.Air;
    public const ushort Stone = Block.Stone;
    public const ushort Deepslate = Block.Deepslate;
    public const ushort Bedrock = Block.Bedrock;
    public const ushort GrassBlock = Block.GrassBlock;
    public const ushort Dirt = Block.Dirt;
    public const ushort Sand = Block.Sand;
    public const ushort Gravel = Block.Gravel;
    public const ushort Water = Block.Water;

    public static void BuildSurface(Chunk chunk, int x, int z, int highestSolidY, BiomeType biome, Random random)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(random);

        // 1. Bedrock floor (Y = -64 to -60)
        chunk.SetBlockState(x, -64, z, Block.Bedrock);
        if (random.Next(100) < 80) chunk.SetBlockState(x, -63, z, Block.Bedrock);
        if (random.Next(100) < 50) chunk.SetBlockState(x, -62, z, Block.Bedrock);
        if (random.Next(100) < 20) chunk.SetBlockState(x, -61, z, Block.Bedrock);

        if (highestSolidY < -64)
        {
            // Void column below bedrock: fill water up to sea level (63)
            for (int y = -63; y <= 63; y++)
            {
                if (chunk.GetBlockState(x, y, z) == Block.Air)
                    chunk.SetBlockState(x, y, z, Block.Water);
            }
            return;
        }

        // 2. Deepslate below Y = 0 (transition zone between -8 and 0)
        for (int y = -63; y <= Math.Min(highestSolidY, 0); y++)
        {
            ushort current = chunk.GetBlockState(x, y, z);
            if (current == Block.Stone)
            {
                if (y < -8)
                {
                    chunk.SetBlockState(x, y, z, Block.Deepslate);
                }
                else
                {
                    int deepslateChance = (-y) * 12; // -8 -> ~96%, -1 -> ~12%
                    if (random.Next(100) < deepslateChance)
                    {
                        chunk.SetBlockState(x, y, z, Block.Deepslate);
                    }
                }
            }
        }

        // 3. Above sea level (Y >= 63)
        if (highestSolidY >= 63)
        {
            switch (biome)
            {
                case BiomeType.Beach:
                {
                    // Golden beach sand dunes
                    chunk.SetBlockState(x, highestSolidY, z, Block.Sand);
                    for (int d = 1; d <= 3; d++)
                    {
                        int subY = highestSolidY - d;
                        if (subY > -64 && chunk.GetBlockState(x, subY, z) == Block.Stone)
                            chunk.SetBlockState(x, subY, z, Block.Sand);
                    }
                    // Layer of sandstone under sand
                    int stoneY = highestSolidY - 4;
                    if (stoneY > -64 && chunk.GetBlockState(x, stoneY, z) == Block.Stone)
                        chunk.SetBlockState(x, stoneY, z, Block.Sandstone);
                    break;
                }

                case BiomeType.Desert:
                {
                    // 4 layers of sand, followed by 3 layers of sandstone
                    chunk.SetBlockState(x, highestSolidY, z, Block.Sand);
                    for (int d = 1; d <= 3; d++)
                    {
                        int subY = highestSolidY - d;
                        if (subY > -64 && chunk.GetBlockState(x, subY, z) == Block.Stone)
                            chunk.SetBlockState(x, subY, z, Block.Sand);
                    }
                    for (int d = 4; d <= 6; d++)
                    {
                        int subY = highestSolidY - d;
                        if (subY > -64 && chunk.GetBlockState(x, subY, z) == Block.Stone)
                            chunk.SetBlockState(x, subY, z, Block.Sandstone);
                    }
                    break;
                }

                case BiomeType.JaggedPeaks or BiomeType.SnowySlopes or BiomeType.Mountains:
                {
                    if (highestSolidY > 105)
                    {
                        // High peaks covered with Snow Block and Snow layer
                        chunk.SetBlockState(x, highestSolidY, z, Block.SnowBlock);
                        chunk.SetBlockState(x, highestSolidY + 1, z, Block.Snow);
                        for (int d = 1; d <= 2; d++)
                        {
                            int subY = highestSolidY - d;
                            if (subY > -64 && chunk.GetBlockState(x, subY, z) == Block.Stone)
                                chunk.SetBlockState(x, subY, z, Block.SnowBlock);
                        }
                    }
                    else if (highestSolidY > 88)
                    {
                        // Exposed mountain stone with snow patches
                        ushort top = (random.Next(100) < 45) ? Block.SnowBlock : Block.Stone;
                        chunk.SetBlockState(x, highestSolidY, z, top);
                        if (top == Block.SnowBlock)
                            chunk.SetBlockState(x, highestSolidY + 1, z, Block.Snow);
                    }
                    else
                    {
                        // Lower mountain slopes: Grass block
                        chunk.SetBlockState(x, highestSolidY, z, Block.GrassBlock);
                        for (int d = 1; d <= 3; d++)
                        {
                            int subY = highestSolidY - d;
                            if (subY > -64 && chunk.GetBlockState(x, subY, z) == Block.Stone)
                                chunk.SetBlockState(x, subY, z, Block.Dirt);
                        }
                    }
                    break;
                }

                case BiomeType.Taiga:
                {
                    // Coniferous ground: Grass, Podzol, and Coarse Dirt
                    int roll = random.Next(100);
                    ushort top = (roll < 45) ? Block.GrassBlock : (roll < 80) ? Block.Podzol : Block.CoarseDirt;
                    chunk.SetBlockState(x, highestSolidY, z, top);
                    for (int d = 1; d <= 3; d++)
                    {
                        int subY = highestSolidY - d;
                        if (subY > -64 && chunk.GetBlockState(x, subY, z) == Block.Stone)
                            chunk.SetBlockState(x, subY, z, Block.Dirt);
                    }
                    break;
                }

                case BiomeType.Badlands:
                {
                    // Grand Canyon Terracotta strata layering
                    chunk.SetBlockState(x, highestSolidY, z, (random.Next(100) < 40) ? Block.RedSand : Block.Terracotta);
                    for (int y = highestSolidY; y >= 60; y--)
                    {
                        if (chunk.GetBlockState(x, y, z) == Block.Stone)
                        {
                            int band = Math.Abs(y) % 16;
                            ushort terracotta = band switch
                            {
                                0 or 1 => Block.OrangeTerracotta,
                                2 or 3 => Block.Terracotta,
                                4 or 5 => Block.YellowTerracotta,
                                6 or 7 => Block.WhiteTerracotta,
                                8 or 9 => Block.BrownTerracotta,
                                10 or 11 => Block.RedTerracotta,
                                12 => Block.LightGrayTerracotta,
                                _ => Block.OrangeTerracotta
                            };
                            chunk.SetBlockState(x, y, z, terracotta);
                        }
                    }
                    break;
                }

                case BiomeType.Swamp:
                {
                    // Muddy lowland with pools of water
                    int roll = random.Next(100);
                    ushort top = (roll < 55) ? Block.GrassBlock : (roll < 90) ? Block.Mud : Block.Dirt;
                    chunk.SetBlockState(x, highestSolidY, z, top);
                    for (int d = 1; d <= 3; d++)
                    {
                        int subY = highestSolidY - d;
                        if (subY > -64 && chunk.GetBlockState(x, subY, z) == Block.Stone)
                            chunk.SetBlockState(x, subY, z, (d <= 2) ? Block.Mud : Block.Dirt);
                    }
                    break;
                }

                case BiomeType.Savanna:
                {
                    // Dry grassland with coarse dirt patches
                    int roll = random.Next(100);
                    ushort top = (roll < 75) ? Block.GrassBlock : Block.CoarseDirt;
                    chunk.SetBlockState(x, highestSolidY, z, top);
                    for (int d = 1; d <= 3; d++)
                    {
                        int subY = highestSolidY - d;
                        if (subY > -64 && chunk.GetBlockState(x, subY, z) == Block.Stone)
                            chunk.SetBlockState(x, subY, z, Block.Dirt);
                    }
                    break;
                }

                case BiomeType.Jungle:
                {
                    // Lush rainforest floor
                    int roll = random.Next(100);
                    ushort top = (roll < 80) ? Block.GrassBlock : Block.Podzol;
                    chunk.SetBlockState(x, highestSolidY, z, top);
                    for (int d = 1; d <= 3; d++)
                    {
                        int subY = highestSolidY - d;
                        if (subY > -64 && chunk.GetBlockState(x, subY, z) == Block.Stone)
                            chunk.SetBlockState(x, subY, z, Block.Dirt);
                    }
                    break;
                }

                case BiomeType.DarkForest:
                {
                    // Deep dark loam floor
                    chunk.SetBlockState(x, highestSolidY, z, Block.GrassBlock);
                    for (int d = 1; d <= 4; d++)
                    {
                        int subY = highestSolidY - d;
                        if (subY > -64 && chunk.GetBlockState(x, subY, z) == Block.Stone)
                            chunk.SetBlockState(x, subY, z, (d == 1 && random.Next(100) < 30) ? Block.CoarseDirt : Block.Dirt);
                    }
                    break;
                }

                default: // Plains, Forest, BirchForest, etc.
                {
                    // Shoreline transition: if right at water level (63-64), chance of sand shore
                    if (highestSolidY <= 64 && random.Next(100) < 35)
                    {
                        chunk.SetBlockState(x, highestSolidY, z, Block.Sand);
                        for (int d = 1; d <= 2; d++)
                        {
                            int subY = highestSolidY - d;
                            if (subY > -64 && chunk.GetBlockState(x, subY, z) == Block.Stone)
                                chunk.SetBlockState(x, subY, z, Block.Sand);
                        }
                    }
                    else
                    {
                        chunk.SetBlockState(x, highestSolidY, z, Block.GrassBlock);
                        for (int d = 1; d <= 3; d++)
                        {
                            int subY = highestSolidY - d;
                            if (subY > -64 && chunk.GetBlockState(x, subY, z) == Block.Stone)
                                chunk.SetBlockState(x, subY, z, Block.Dirt);
                        }
                    }
                    break;
                }
            }
        }
        // 4. Underwater / Ocean Floor (highestSolidY < 63)
        else
        {
            bool isDeep = (highestSolidY <= 44 || biome == BiomeType.DeepOcean);

            ushort floorBlock;
            if (isDeep)
            {
                // Deep ocean floor: predominantly gravel and stone, occasional magma vents
                int roll = random.Next(100);
                if (roll < 70) floorBlock = Block.Gravel;
                else if (roll < 95) floorBlock = Block.Stone;
                else floorBlock = Block.MagmaBlock; // Hydrothermal vent!
            }
            else
            {
                // Shallow ocean / coastal waters: smooth sand beds, gravel, and clay clusters
                int roll = random.Next(100);
                if (roll < 65) floorBlock = Block.Sand;
                else if (roll < 85) floorBlock = Block.Gravel;
                else floorBlock = Block.Clay; // Natural clay patches
            }

            chunk.SetBlockState(x, highestSolidY, z, floorBlock);

            // Subsurface sediment layers (2 blocks)
            ushort subFloor = (floorBlock == Block.MagmaBlock) ? Block.Stone : floorBlock;
            for (int d = 1; d <= 2; d++)
            {
                int subY = highestSolidY - d;
                if (subY > -64 && chunk.GetBlockState(x, subY, z) == Block.Stone)
                {
                    chunk.SetBlockState(x, subY, z, subFloor);
                }
            }

            // Fill water from highestSolidY + 1 up to sea level (63)
            for (int wy = highestSolidY + 1; wy <= 63; wy++)
            {
                chunk.SetBlockState(x, wy, z, Block.Water);
            }

            // Marine flora (Seagrass and Kelp)
            int waterDepth = 63 - highestSolidY;
            if (waterDepth >= 2 && (floorBlock == Block.Sand || floorBlock == Block.Gravel))
            {
                int floraRoll = random.Next(1000);
                if (floraRoll < 120) // 12% Seagrass
                {
                    chunk.SetBlockState(x, highestSolidY + 1, z, Block.Seagrass);
                }
                else if (floraRoll < 160 && waterDepth >= 5 && !isDeep) // 4% Kelp forest
                {
                    int kelpHeight = random.Next(3, Math.Min(12, waterDepth - 1));
                    for (int ky = 1; ky < kelpHeight; ky++)
                    {
                        chunk.SetBlockState(x, highestSolidY + ky, z, Block.KelpPlant);
                    }
                    chunk.SetBlockState(x, highestSolidY + kelpHeight, z, Block.Kelp);
                }
            }
        }
    }
}

