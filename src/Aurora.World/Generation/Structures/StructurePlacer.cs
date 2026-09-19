using System;
using Aurora.World;

namespace Aurora.World.Generation.Structures;

/// <summary>
/// Generates authentic vanilla structures:
/// 1. Ruined Nether Portal with obsidian frame, crying obsidian, netherrack bleeding, magma, gold block, and chest.
/// 2. Plains Village House with Adaptive Foundation (never floats!), corner oak logs, glass windows, oak stairs gable roof, crafting table, chest, and torches.
/// 3. Desert Well with sandstone columns and water pool.
/// 4. Explorer Campsite with active campfire, log benches, hay bale, chest, and lantern.
/// </summary>
public static class StructurePlacer
{
    public static void PlaceStructures(Chunk chunk, int chunkX, int chunkZ, int[] highestSolidY, BiomeType biome, int seed)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(highestSolidY);

        // Deterministic structure hash
        uint hash = (uint)(chunkX * 341873128712L ^ chunkZ * 132897987541L ^ seed);
        hash ^= hash >> 13;
        hash *= 1274126177;
        hash ^= hash >> 16;

        // 1. Ruined Nether Portal (~1 in 36 chunks, land only)
        if ((hash % 36) == 0)
        {
            TryPlaceRuinedPortal(chunk, highestSolidY, hash);
        }
        // 2. Desert Well (~1 in 30 chunks in Desert)
        else if (biome == BiomeType.Desert && (hash % 30) == 1)
        {
            TryPlaceDesertWell(chunk, highestSolidY);
        }
        // 3. Desert Pyramid (~1 in 32 chunks in Desert or Badlands)
        else if ((biome == BiomeType.Desert || biome == BiomeType.Badlands) && (hash % 32) == 2)
        {
            TryPlaceDesertPyramid(chunk, highestSolidY);
        }
        // 4. Jungle Temple (~1 in 28 chunks in Jungle)
        else if (biome == BiomeType.Jungle && (hash % 28) == 3)
        {
            TryPlaceJungleTemple(chunk, highestSolidY);
        }
        // 5. Swamp Hut (~1 in 24 chunks in Swamp)
        else if (biome == BiomeType.Swamp && (hash % 24) == 4)
        {
            TryPlaceSwampHut(chunk, highestSolidY);
        }
        // 6. Shipwreck (~1 in 28 chunks in Ocean or Beach)
        else if ((biome == BiomeType.Ocean || biome == BiomeType.DeepOcean || biome == BiomeType.Beach) && (hash % 28) == 5)
        {
            TryPlaceShipwreck(chunk, highestSolidY, hash);
        }
        // 7. Explorer Campsite (~1 in 24 chunks in Taiga, Forest, or Plains)
        else if ((biome == BiomeType.Taiga || biome == BiomeType.Forest) && (hash % 24) == 6)
        {
            TryPlaceCampsite(chunk, highestSolidY, hash);
        }
        // 8. Plains Village House (~1 in 28 chunks in Plains or Forest)
        else if ((biome == BiomeType.Plains || biome == BiomeType.Forest) && (hash % 28) == 7)
        {
            TryPlaceVillageHouse(chunk, highestSolidY);
        }
    }

    /// <summary>
    /// Ruined Nether Portal with obsidian frame, crying obsidian, gold block, and netherrack bleed.
    /// </summary>
    private static void TryPlaceRuinedPortal(Chunk chunk, int[] highestSolidY, uint hash)
    {
        int cx = 8;
        int cz = 8;
        int baseY = highestSolidY[cx * 16 + cz];
        if (baseY < 64 || baseY > 130) return;

        // 1. Netherrack & Magma bleeding floor (radius 4)
        for (int dx = -4; dx <= 4; dx++)
        {
            for (int dz = -4; dz <= 4; dz++)
            {
                if (dx * dx + dz * dz > 16) continue;
                int px = cx + dx;
                int pz = cz + dz;
                if (px < 0 || px > 15 || pz < 0 || pz > 15) continue;

                int ground = highestSolidY[px * 16 + pz];
                if (ground < 63) continue;

                uint r = (uint)(dx * 31 + dz * 17 + (int)hash);
                ushort floor = ((r & 7) == 0) ? Block.MagmaBlock :
                               ((r & 3) == 0) ? Block.Netherrack :
                               ((r & 5) == 0) ? Block.StoneBricks : Block.Netherrack;
                chunk.SetBlockState(px, ground, pz, floor);
            }
        }

        // 2. Portal Frame: 4 wide x 5 high at (cx - 2..cx + 1, cz)
        int startX = cx - 2;
        int portalZ = cz;

        for (int w = 0; w < 4; w++)
        {
            for (int h = 0; h < 5; h++)
            {
                int px = startX + w;
                int py = baseY + 1 + h;
                if (px < 0 || px > 15) continue;

                bool isFrame = (w == 0 || w == 3 || h == 0 || h == 4);
                if (isFrame)
                {
                    // Frame block: some crying obsidian, occasional missing ruined block
                    if ((w == 3 && h == 4) || (w == 0 && h == 2))
                    {
                        // Ruined gap in the frame
                        continue;
                    }

                    ushort obs = ((w + h) % 3 == 0) ? Block.CryingObsidian : Block.Obsidian;
                    chunk.SetBlockState(px, py, portalZ, obs);
                }
                else
                {
                    // Portal air opening
                    chunk.SetBlockState(px, py, portalZ, Block.Air);
                }
            }
        }

        // 3. Loot chest & Gold Block
        chunk.SetBlockState(cx + 2, baseY + 1, cz + 1, Block.GoldBlock);
        chunk.SetBlockState(cx - 2, baseY + 1, cz - 1, Block.Chest);

        // 4. Ruined Stone Brick Pillar
        chunk.SetBlockState(cx + 3, baseY + 1, cz, Block.StoneBricks);
        chunk.SetBlockState(cx + 3, baseY + 2, cz, Block.MossyStoneBricks);
        chunk.SetBlockState(cx + 3, baseY + 3, cz, Block.CrackedStoneBricks);
    }

    /// <summary>
    /// Authentic Plains Village House with Adaptive Foundation, Gable Stair Roof, and interior decor.
    /// </summary>
    private static void TryPlaceVillageHouse(Chunk chunk, int[] highestSolidY)
    {
        // 6x6 footprint (x: 5..10, z: 5..10)
        int baseY = highestSolidY[5 * 16 + 5];
        if (baseY < 64 || baseY > 120) return;

        // Check slope - cannot be more than 4 blocks height variation
        for (int x = 5; x <= 10; x++)
        {
            for (int z = 5; z <= 10; z++)
            {
                int y = highestSolidY[x * 16 + z];
                if (Math.Abs(y - baseY) > 4) return;
            }
        }

        // 1. Adaptive Foundation: fill cobblestone from baseY down to solid ground for the entire 6x6 area
        for (int x = 5; x <= 10; x++)
        {
            for (int z = 5; z <= 10; z++)
            {
                int solidY = highestSolidY[x * 16 + z];
                for (int y = baseY; y >= solidY; y--)
                {
                    chunk.SetBlockState(x, y, z, Block.Cobblestone);
                }
            }
        }

        // 2. Walls (baseY + 1 to baseY + 4)
        for (int y = baseY + 1; y <= baseY + 4; y++)
        {
            for (int x = 5; x <= 10; x++)
            {
                for (int z = 5; z <= 10; z++)
                {
                    bool isCorner = (x == 5 || x == 10) && (z == 5 || z == 10);
                    bool isWall = (x == 5 || x == 10 || z == 5 || z == 10);

                    if (isCorner)
                    {
                        // Corner logs
                        chunk.SetBlockState(x, y, z, Block.OakLog);
                    }
                    else if (isWall)
                    {
                        // Lower wall layer = Cobblestone, upper = Oak Planks
                        if (y == baseY + 1)
                        {
                            if (x == 7 && z == 5) // Entrance
                                chunk.SetBlockState(x, y, z, Block.OakDoor);
                            else
                                chunk.SetBlockState(x, y, z, Block.Cobblestone);
                        }
                        else if (y == baseY + 2)
                        {
                            // Door top opening or Glass windows
                            if (x == 7 && z == 5)
                            {
                                chunk.SetBlockState(x, y, z, Block.Air);
                            }
                            else if ((x == 7 && z == 10) || (z == 7 && (x == 5 || x == 10)))
                            {
                                chunk.SetBlockState(x, y, z, Block.Glass);
                            }
                            else
                            {
                                chunk.SetBlockState(x, y, z, Block.OakPlanks);
                            }
                        }
                        else
                        {
                            chunk.SetBlockState(x, y, z, Block.OakPlanks);
                        }
                    }
                    else
                    {
                        // Hollow interior
                        chunk.SetBlockState(x, y, z, Block.Air);
                    }
                }
            }
        }

        // 3. Interior furniture: Crafting Table, Chest, Wall Torch
        chunk.SetBlockState(6, baseY + 1, 9, Block.CraftingTable);
        chunk.SetBlockState(9, baseY + 1, 9, Block.Chest);
        chunk.SetBlockState(7, baseY + 3, 9, Block.Torch);
        chunk.SetBlockState(7, baseY + 3, 4, Block.Torch); // Porch torch

        // 4. Gable Stair Roof with overhang
        // Eaves overhang (x = 4 and x = 11, z: 4..11)
        for (int z = 4; z <= 11; z++)
        {
            // Level 1 of roof (baseY + 4)
            chunk.SetBlockState(4, baseY + 4, z, Block.OakStairs);
            chunk.SetBlockState(11, baseY + 4, z, Block.OakStairs);

            // Level 2 of roof (baseY + 5)
            chunk.SetBlockState(5, baseY + 5, z, Block.OakStairs);
            chunk.SetBlockState(10, baseY + 5, z, Block.OakStairs);

            // Level 3 of roof (baseY + 6)
            chunk.SetBlockState(6, baseY + 6, z, Block.OakStairs);
            chunk.SetBlockState(9, baseY + 6, z, Block.OakStairs);

            // Roof Ridge (baseY + 6)
            chunk.SetBlockState(7, baseY + 6, z, Block.OakPlanks);
            chunk.SetBlockState(8, baseY + 6, z, Block.OakPlanks);
        }

        // Front porch steps
        chunk.SetBlockState(7, baseY, 4, Block.CobblestoneStairs);
    }

    /// <summary>
    /// Desert Well structure with sandstone base, columns, and central water pool.
    /// </summary>
    private static void TryPlaceDesertWell(Chunk chunk, int[] highestSolidY)
    {
        int cx = 7;
        int cz = 7;
        int baseY = highestSolidY[cx * 16 + cz];
        if (baseY < 64 || baseY > 110) return;

        // 5x5 Sandstone well
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dz = -2; dz <= 2; dz++)
            {
                int x = cx + dx;
                int z = cz + dz;
                bool isBorder = (Math.Abs(dx) == 2 || Math.Abs(dz) == 2);

                if (isBorder)
                {
                    chunk.SetBlockState(x, baseY, z, Block.Sandstone);
                    chunk.SetBlockState(x, baseY + 1, z, Block.Sandstone);
                }
                else
                {
                    // Interior water pool
                    chunk.SetBlockState(x, baseY - 1, z, Block.Sandstone);
                    chunk.SetBlockState(x, baseY, z, Block.Water);
                    chunk.SetBlockState(x, baseY + 1, z, Block.Air);
                }
            }
        }

        // 4 corner pillars
        chunk.SetBlockState(cx - 2, baseY + 2, cz - 2, Block.Sandstone);
        chunk.SetBlockState(cx - 2, baseY + 3, cz - 2, Block.Sandstone);
        chunk.SetBlockState(cx + 2, baseY + 2, cz - 2, Block.Sandstone);
        chunk.SetBlockState(cx + 2, baseY + 3, cz - 2, Block.Sandstone);
        chunk.SetBlockState(cx - 2, baseY + 2, cz + 2, Block.Sandstone);
        chunk.SetBlockState(cx - 2, baseY + 3, cz + 2, Block.Sandstone);
        chunk.SetBlockState(cx + 2, baseY + 2, cz + 2, Block.Sandstone);
        chunk.SetBlockState(cx + 2, baseY + 3, cz + 2, Block.Sandstone);

        // Roof slab
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dz = -2; dz <= 2; dz++)
            {
                chunk.SetBlockState(cx + dx, baseY + 4, cz + dz, Block.SmoothSandstone);
            }
        }
    }

    /// <summary>
    /// Explorer Campsite with burning campfire, log benches, hay bale, chest, and lantern.
    /// </summary>
    private static void TryPlaceCampsite(Chunk chunk, int[] highestSolidY, uint hash)
    {
        int cx = 8;
        int cz = 8;
        int baseY = highestSolidY[cx * 16 + cz];
        if (baseY < 64 || baseY > 120) return;

        // Clear small circular clearing
        chunk.SetBlockState(cx, baseY + 1, cz, Block.Campfire);

        // Log seating around the fire
        chunk.SetBlockState(cx - 2, baseY + 1, cz, Block.OakLog);
        chunk.SetBlockState(cx + 2, baseY + 1, cz, Block.OakLog);
        chunk.SetBlockState(cx, baseY + 1, cz - 2, Block.OakLog);

        // Supply chest and hay stack
        chunk.SetBlockState(cx + 2, baseY + 1, cz + 2, Block.Chest);
        chunk.SetBlockState(cx - 2, baseY + 1, cz + 2, Block.HayBlock);
        chunk.SetBlockState(cx - 2, baseY + 2, cz + 2, Block.HayBlock);

        // Lantern post
        chunk.SetBlockState(cx + 2, baseY + 1, cz - 2, Block.OakFence);
        chunk.SetBlockState(cx + 2, baseY + 2, cz - 2, Block.OakFence);
        chunk.SetBlockState(cx + 2, baseY + 3, cz - 2, Block.Lantern);
    }

    /// <summary>
    /// Desert Pyramid with sandstone steps, terracotta accents, and a secret underground TNT trap room.
    /// </summary>
    private static void TryPlaceDesertPyramid(Chunk chunk, int[] highestSolidY)
    {
        int cx = 8;
        int cz = 8;
        int baseY = highestSolidY[cx * 16 + cz];
        if (baseY < 64 || baseY > 110) return;

        // 1. Pyramid Stepped Exterior (9x9 base stepping up 4 levels)
        for (int step = 0; step < 4; step++)
        {
            int r = 4 - step;
            int y = baseY + step;
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    int px = cx + dx;
                    int pz = cz + dz;
                    if (px >= 0 && px < 16 && pz >= 0 && pz < 16)
                    {
                        ushort block = (step == 1 && (Math.Abs(dx) == r || Math.Abs(dz) == r)) ? Block.OrangeTerracotta : Block.Sandstone;
                        chunk.SetBlockState(px, y, pz, block);
                    }
                }
            }
        }

        // 2. Hollow interior chamber
        for (int y = baseY + 1; y <= baseY + 3; y++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    chunk.SetBlockState(cx + dx, y, cz + dz, Block.Air);
                }
            }
        }

        // Center blue terracotta floor emblem
        chunk.SetBlockState(cx, baseY, cz, Block.ChiseledSandstone);

        // 3. Secret subterranean chamber (8 blocks down) with TNT trap & 4 Loot Chests
        int secretFloorY = baseY - 8;
        if (secretFloorY > -50)
        {
            // Hollow drop shaft
            for (int sy = baseY; sy >= secretFloorY; sy--)
            {
                chunk.SetBlockState(cx, sy, cz, Block.Air);
            }

            // Secret 5x5 chamber
            for (int dy = 0; dy <= 3; dy++)
            {
                int cy = secretFloorY + dy;
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        int px = cx + dx;
                        int pz = cz + dz;
                        if (dy == 0)
                        {
                            chunk.SetBlockState(px, cy, pz, Block.CutSandstone);
                        }
                        else
                        {
                            chunk.SetBlockState(px, cy, pz, Block.Air);
                        }
                    }
                }
            }

            // Pressure plate in center, TNT underneath
            chunk.SetBlockState(cx, secretFloorY + 1, cz, Block.StonePressurePlate);
            chunk.SetBlockState(cx, secretFloorY, cz, Block.Tnt);

            // 4 Chests in corners
            chunk.SetBlockState(cx - 2, secretFloorY + 1, cz - 2, Block.Chest);
            chunk.SetBlockState(cx + 2, secretFloorY + 1, cz - 2, Block.Chest);
            chunk.SetBlockState(cx - 2, secretFloorY + 1, cz + 2, Block.Chest);
            chunk.SetBlockState(cx + 2, secretFloorY + 1, cz + 2, Block.Chest);
        }
    }

    /// <summary>
    /// Jungle Temple with mossy cobblestone, vines, tripwire dispenser traps, and loot chests.
    /// </summary>
    private static void TryPlaceJungleTemple(Chunk chunk, int[] highestSolidY)
    {
        int cx = 8;
        int cz = 8;
        int baseY = highestSolidY[cx * 16 + cz];
        if (baseY < 64 || baseY > 115) return;

        // 7x7 stone/mossy cobblestone temple
        for (int y = baseY; y <= baseY + 4; y++)
        {
            for (int dx = -3; dx <= 3; dx++)
            {
                for (int dz = -3; dz <= 3; dz++)
                {
                    int px = cx + dx;
                    int pz = cz + dz;
                    bool isWall = Math.Abs(dx) == 3 || Math.Abs(dz) == 3;
                    bool isRoof = (y == baseY + 4);
                    bool isFloor = (y == baseY);

                    if (isWall || isRoof || isFloor)
                    {
                        ushort b = ((dx + dz + y) % 3 == 0) ? Block.MossyCobblestone : Block.Cobblestone;
                        chunk.SetBlockState(px, y, pz, b);
                    }
                    else
                    {
                        chunk.SetBlockState(px, y, pz, Block.Air);
                    }
                }
            }
        }

        // Entrance slit
        chunk.SetBlockState(cx, baseY + 1, cz - 3, Block.Air);
        chunk.SetBlockState(cx, baseY + 2, cz - 3, Block.Air);

        // Altar with chest and hidden dispenser trap
        chunk.SetBlockState(cx, baseY + 1, cz + 2, Block.Chest);
        chunk.SetBlockState(cx, baseY + 1, cz, Block.Tripwire);
        chunk.SetBlockState(cx - 2, baseY + 1, cz, Block.TripwireHook);
        chunk.SetBlockState(cx + 2, baseY + 1, cz, Block.TripwireHook);
        chunk.SetBlockState(cx + 3, baseY + 1, cz, Block.Dispenser);

        // Hanging vines on walls
        chunk.SetBlockState(cx - 3, baseY + 2, cz - 2, Block.Vine);
        chunk.SetBlockState(cx + 3, baseY + 3, cz + 1, Block.Vine);
    }

    /// <summary>
    /// Swamp Hut: Elevated stilt hut over water or mud with cauldron, crafting table, and flower pot.
    /// </summary>
    private static void TryPlaceSwampHut(Chunk chunk, int[] highestSolidY)
    {
        int cx = 8;
        int cz = 8;
        int baseY = highestSolidY[cx * 16 + cz];
        if (baseY < 60 || baseY > 75) return;

        int floorY = baseY + 3; // Elevated 3 blocks above water/mud

        // 4 Corner stilt pillars extending down to solid ground
        int[] cornersX = { cx - 3, cx + 3, cx - 3, cx + 3 };
        int[] cornersZ = { cz - 3, cz - 3, cz + 3, cz + 3 };

        for (int i = 0; i < 4; i++)
        {
            int colX = cornersX[i];
            int colZ = cornersZ[i];
            for (int y = floorY; y >= baseY - 4; y--)
            {
                if (y < -64) break;
                ushort b = chunk.GetBlockState(colX, y, colZ);
                chunk.SetBlockState(colX, y, colZ, Block.OakLog);
                if (b == Block.Dirt || b == Block.GrassBlock || b == Block.Mud || b == Block.Clay) break;
            }
        }

        // 7x7 Plank Floor
        for (int dx = -3; dx <= 3; dx++)
        {
            for (int dz = -3; dz <= 3; dz++)
            {
                chunk.SetBlockState(cx + dx, floorY, cz + dz, Block.OakPlanks);
            }
        }

        // 7x7 Hut Walls & Ceiling
        for (int dy = 1; dy <= 3; dy++)
        {
            int y = floorY + dy;
            for (int dx = -3; dx <= 3; dx++)
            {
                for (int dz = -3; dz <= 3; dz++)
                {
                    bool isEdge = Math.Abs(dx) == 3 || Math.Abs(dz) == 3;
                    if (isEdge)
                    {
                        // Window openings on sides
                        if (dy == 2 && (dx == 0 || dz == 0))
                        {
                            chunk.SetBlockState(cx + dx, y, cz + dz, Block.Air);
                        }
                        else
                        {
                            chunk.SetBlockState(cx + dx, y, cz + dz, Block.OakPlanks);
                        }
                    }
                    else
                    {
                        chunk.SetBlockState(cx + dx, y, cz + dz, Block.Air);
                    }
                }
            }
        }

        // Roof overhang
        for (int dx = -4; dx <= 4; dx++)
        {
            for (int dz = -4; dz <= 4; dz++)
            {
                chunk.SetBlockState(cx + dx, floorY + 4, cz + dz, Block.OakPlanks);
            }
        }

        // Doorway
        chunk.SetBlockState(cx, floorY + 1, cz - 3, Block.Air);
        chunk.SetBlockState(cx, floorY + 2, cz - 3, Block.Air);

        // Furniture: Cauldron, Crafting Table, Flower Pot
        chunk.SetBlockState(cx + 2, floorY + 1, cz + 2, Block.Cauldron);
        chunk.SetBlockState(cx - 2, floorY + 1, cz + 2, Block.CraftingTable);
        chunk.SetBlockState(cx - 2, floorY + 1, cz - 2, Block.FlowerPot);
        chunk.SetBlockState(cx, floorY + 3, cz, Block.Torch);
    }

    /// <summary>
    /// Shipwreck: Tilted wooden hull with broken mast and captain's treasure chest.
    /// </summary>
    private static void TryPlaceShipwreck(Chunk chunk, int[] highestSolidY, uint hash)
    {
        int cx = 8;
        int cz = 8;
        int baseY = highestSolidY[cx * 16 + cz];
        if (baseY < 35 || baseY > 70) return;

        int shipLen = 10;
        int startZ = cz - (shipLen / 2);

        // Hull Keel & Ribs
        for (int i = 0; i < shipLen; i++)
        {
            int z = startZ + i;
            if (z < 0 || z >= 16) continue;

            // Central Keel
            chunk.SetBlockState(cx, baseY, z, Block.OakLog);

            // Curved ribs
            chunk.SetBlockState(cx - 1, baseY + 1, z, Block.OakPlanks);
            chunk.SetBlockState(cx + 1, baseY + 1, z, Block.OakPlanks);
            chunk.SetBlockState(cx - 2, baseY + 2, z, Block.OakPlanks);
            chunk.SetBlockState(cx + 2, baseY + 2, z, Block.OakPlanks);

            // Deck floor
            if (i >= 2 && i <= shipLen - 2)
            {
                chunk.SetBlockState(cx, baseY + 1, z, Block.OakPlanks);
            }
        }

        // Broken Mast in middle
        int mastHeight = 4 + (int)(hash % 3);
        for (int m = 2; m <= mastHeight; m++)
        {
            chunk.SetBlockState(cx, baseY + m, cz, Block.OakLog);
        }

        // Captain's Quarters Treasure Chest at the stern
        int sternZ = startZ + shipLen - 2;
        if (sternZ >= 0 && sternZ < 16)
        {
            chunk.SetBlockState(cx, baseY + 2, sternZ, Block.Chest);
            chunk.SetBlockState(cx, baseY + 3, sternZ, Block.Lantern);
        }
    }
}

