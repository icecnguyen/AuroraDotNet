using System;
using System.Collections.Generic;

namespace Aurora.World.Lighting;

/// <summary>
/// High-performance 3D Lighting Engine for Minecraft 1.21.4.
/// Computes realistic SkyLight attenuation and BFS flood-fill diffusion,
/// as well as BlockLight emission and propagation from torches, lanterns, and light sources.
/// </summary>
public static class LightEngine
{
    /// <summary>
    /// Returns the light opacity of a given block state (0 = fully transparent, 15 = fully opaque).
    /// </summary>
    public static byte GetOpacity(ushort block)
    {
        return block switch
        {
            Block.Air or Block.Glass => 0,
            Block.ShortGrass or Block.Dandelion or Block.Poppy or Block.Cornflower or Block.Allium => 0,
            Block.Fern or Block.DeadBush or Block.Cactus or Block.SugarCane or Block.LilyPad => 0,
            Block.Seagrass or Block.Kelp or Block.KelpPlant => 0,
            Block.Torch or Block.WallTorch or Block.Lantern or Block.Campfire => 0,
            Block.OakDoor or Block.OakFence or Block.OakStairs or Block.CobblestoneStairs or Block.Chest or Block.Snow => 0,
            Block.OakLeaves or Block.SpruceLeaves or Block.BirchLeaves => 1, // Filtered light under trees
            Block.Water => 2,
            Block.Ice or Block.PackedIce => 2,
            _ => 15 // Fully opaque (Stone, Dirt, Cobblestone, Logs, Planks, Bedrock, etc.)
        };
    }

    /// <summary>
    /// Returns the block light emission of a given block state (0 to 15).
    /// </summary>
    public static byte GetEmission(ushort block)
    {
        return block switch
        {
            Block.Lantern or Block.Lava or Block.Campfire => 15,
            Block.Torch or Block.WallTorch => 14,
            Block.CryingObsidian => 10,
            Block.MagmaBlock => 3,
            _ => 0
        };
    }

    /// <summary>
    /// Computes full 3D SkyLight and BlockLight for a chunk.
    /// </summary>
    public static void InitializeLighting(Chunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        // Fast reusable integer queue for BFS diffusion (stores packed: (y + 64) << 8 | z << 4 | x)
        var skyQueue = new Queue<int>(256);
        var blockQueue = new Queue<int>(64);

        // 1. Downward raycast for SkyLight + find initial light sources
        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                byte currentSky = 15;

                for (int y = 319; y >= -64; y--)
                {
                    ushort block = chunk.GetBlockState(x, y, z);
                    byte opacity = GetOpacity(block);
                    byte emission = GetEmission(block);

                    // Block Light Source
                    if (emission > 0)
                    {
                        chunk.SetBlockLight(x, y, z, emission);
                        blockQueue.Enqueue(PackCoord(x, y, z));
                    }
                    else
                    {
                        chunk.SetBlockLight(x, y, z, 0);
                    }

                    // Sky Light
                    if (opacity >= 15)
                    {
                        currentSky = 0;
                    }
                    else
                    {
                        currentSky = (byte)Math.Max(0, currentSky - opacity);
                    }

                    chunk.SetSkyLight(x, y, z, currentSky);

                    // If light is bright enough (>= 14) and near an overhang/tree border, prepare for horizontal diffusion
                    if (currentSky >= 14 && y < 300)
                    {
                        skyQueue.Enqueue(PackCoord(x, y, z));
                    }
                }
            }
        }

        // 2. BFS 3D Flood Fill Diffusion for SkyLight (under trees, overhangs)
        // Limits spread to light level >= 8 for sub-millisecond execution
        while (skyQueue.Count > 0)
        {
            int packed = skyQueue.Dequeue();
            UnpackCoord(packed, out int x, out int y, out int z);
            byte light = chunk.GetSkyLight(x, y, z);
            if (light <= 8) continue;

            // 4 horizontal neighbors
            TryPropagateSky(chunk, x + 1, y, z, light, skyQueue);
            TryPropagateSky(chunk, x - 1, y, z, light, skyQueue);
            TryPropagateSky(chunk, x, y, z + 1, light, skyQueue);
            TryPropagateSky(chunk, x, y, z - 1, light, skyQueue);
            // Downward neighbor
            if (y > -64)
            {
                TryPropagateSky(chunk, x, y - 1, z, light, skyQueue);
            }
        }

        // 3. BFS 3D Flood Fill Diffusion for BlockLight (Torches, Lanterns)
        while (blockQueue.Count > 0)
        {
            int packed = blockQueue.Dequeue();
            UnpackCoord(packed, out int x, out int y, out int z);
            byte light = chunk.GetBlockLight(x, y, z);
            if (light <= 1) continue;

            // 6-directional spread
            TryPropagateBlock(chunk, x + 1, y, z, light, blockQueue);
            TryPropagateBlock(chunk, x - 1, y, z, light, blockQueue);
            TryPropagateBlock(chunk, x, y, z + 1, light, blockQueue);
            TryPropagateBlock(chunk, x, y, z - 1, light, blockQueue);
            if (y < 319) TryPropagateBlock(chunk, x, y + 1, z, light, blockQueue);
            if (y > -64) TryPropagateBlock(chunk, x, y - 1, z, light, blockQueue);
        }
    }

    private static void TryPropagateSky(Chunk chunk, int nx, int ny, int nz, byte sourceLight, Queue<int> queue)
    {
        if (nx < 0 || nx > 15 || nz < 0 || nz > 15 || ny < -64 || ny > 319) return;

        byte opacity = GetOpacity(chunk.GetBlockState(nx, ny, nz));
        if (opacity >= 15) return;

        int newLight = sourceLight - Math.Max((byte)1, opacity);
        if (newLight > chunk.GetSkyLight(nx, ny, nz))
        {
            chunk.SetSkyLight(nx, ny, nz, (byte)newLight);
            if (newLight > 8)
            {
                queue.Enqueue(PackCoord(nx, ny, nz));
            }
        }
    }

    private static void TryPropagateBlock(Chunk chunk, int nx, int ny, int nz, byte sourceLight, Queue<int> queue)
    {
        if (nx < 0 || nx > 15 || nz < 0 || nz > 15 || ny < -64 || ny > 319) return;

        byte opacity = GetOpacity(chunk.GetBlockState(nx, ny, nz));
        if (opacity >= 15) return;

        int newLight = sourceLight - Math.Max((byte)1, opacity);
        if (newLight > chunk.GetBlockLight(nx, ny, nz))
        {
            chunk.SetBlockLight(nx, ny, nz, (byte)newLight);
            if (newLight > 1)
            {
                queue.Enqueue(PackCoord(nx, ny, nz));
            }
        }
    }

    private static int PackCoord(int x, int y, int z)
    {
        return ((y + 64) << 8) | (z << 4) | x;
    }

    private static void UnpackCoord(int packed, out int x, out int y, out int z)
    {
        x = packed & 0x0F;
        z = (packed >> 4) & 0x0F;
        y = (packed >> 8) - 64;
    }
}
