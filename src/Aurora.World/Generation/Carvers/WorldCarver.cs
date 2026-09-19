#pragma warning disable CA5394 // Random is insecure

using System;
using Aurora.World.Generation.Aquifers;

namespace Aurora.World.Generation.Carvers;

/// <summary>
/// Common carver helper methods ported from Pumpkin-MC (pumpkin-world/src/generation/carver).
/// Supports filling with Aquifers (subterranean water pockets and deep lava).
/// </summary>
public abstract class WorldCarver
{
    protected static void CarveEllipsoid(
        Chunk chunk,
        int chunkX,
        int chunkZ,
        double cx,
        double cy,
        double cz,
        double radiusH,
        double radiusV,
        AquiferSampler? aquifer = null)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        int chunkStartX = chunkX * 16;
        int chunkStartZ = chunkZ * 16;

        int minX = Math.Max(0, (int)Math.Floor(cx - radiusH) - chunkStartX);
        int maxX = Math.Min(15, (int)Math.Floor(cx + radiusH) - chunkStartX);

        int minY = Math.Max(-58, (int)Math.Floor(cy - radiusV));
        int maxY = Math.Min(256, (int)Math.Floor(cy + radiusV));

        int minZ = Math.Max(0, (int)Math.Floor(cz - radiusH) - chunkStartZ);
        int maxZ = Math.Min(15, (int)Math.Floor(cz + radiusH) - chunkStartZ);

        if (minX > maxX || minZ > maxZ || minY > maxY)
            return;

        double invRadH2 = 1.0 / (radiusH * radiusH);
        double invRadV2 = 1.0 / (radiusV * radiusV);

        for (int bx = minX; bx <= maxX; bx++)
        {
            double worldX = chunkStartX + bx + 0.5;
            double dx = worldX - cx;
            double distH_X = dx * dx * invRadH2;
            if (distH_X >= 1.0) continue;

            for (int bz = minZ; bz <= maxZ; bz++)
            {
                double worldZ = chunkStartZ + bz + 0.5;
                double dz = worldZ - cz;
                double distH = distH_X + dz * dz * invRadH2;
                if (distH >= 1.0) continue;

                for (int by = minY; by <= maxY; by++)
                {
                    double worldY = by + 0.5;
                    double dy = worldY - cy;
                    double distTotal = distH + dy * dy * invRadV2;

                    if (distTotal < 1.0)
                    {
                        ushort current = chunk.GetBlockState(bx, by, bz);

                        // Only carve carveable terrain (stone, deepslate, dirt, gravel, granite, diorite, andesite, tuff)
                        // NEVER carve bedrock!
                        if (current == Block.Stone || current == Block.Deepslate ||
                            current == Block.Granite || current == Block.Diorite ||
                            current == Block.Andesite || current == Block.Tuff ||
                            current == Block.Dirt || current == Block.Gravel ||
                            current == Block.Sandstone)
                        {
                            if (aquifer != null)
                            {
                                ushort fluid = aquifer.GetCarveFluid((int)worldX, by, (int)worldZ);
                                chunk.SetBlockState(bx, by, bz, fluid);
                            }
                            else if (by <= -54)
                            {
                                chunk.SetBlockState(bx, by, bz, Block.Lava); // Lava lakes at bottom
                            }
                            else
                            {
                                chunk.SetBlockState(bx, by, bz, Block.Air);
                            }
                        }
                    }
                }
            }
        }
    }
}
