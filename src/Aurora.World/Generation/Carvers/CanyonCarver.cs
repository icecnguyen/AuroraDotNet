#pragma warning disable CA5394 // Random is insecure

using System;
using Aurora.World.Generation.Aquifers;

namespace Aurora.World.Generation.Carvers;

/// <summary>
/// Canyon / Ravine carver creating steep, long vertical chasms.
/// Ported and adapted from Pumpkin-MC (pumpkin-world/src/generation/carver/canyon.rs).
/// </summary>
public sealed class CanyonCarver : WorldCarver
{
    private readonly long _seed;

    public CanyonCarver(long seed)
    {
        _seed = seed;
    }

    public void Carve(Chunk chunk, int targetChunkX, int targetChunkZ, AquiferSampler? aquifer = null)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        // Scan neighbor chunks within radius 4
        for (int cx = targetChunkX - 4; cx <= targetChunkX + 4; cx++)
        {
            for (int cz = targetChunkZ - 4; cz <= targetChunkZ + 4; cz++)
            {
                long chunkSeed = _seed ^ (cx * 142113289L) ^ (cz * 478291039L);
                var random = new Random((int)(chunkSeed ^ (chunkSeed >> 32)));

                // ~2.5% chance to start a ravine in this chunk
                if (random.Next(1000) < 25)
                {
                    double originX = cx * 16 + random.Next(16);
                    double originY = random.Next(15, 50); // Starts midway up ground
                    double originZ = cz * 16 + random.Next(16);

                    double yaw = random.NextDouble() * Math.PI * 2.0;
                    double pitch = (random.NextDouble() - 0.5) * 0.2;

                    int length = random.Next(80, 130);
                    double widthMultiplier = 2.8 + random.NextDouble() * 1.5;
                    double verticalRadius = 22.0 + random.NextDouble() * 12.0;

                    double curX = originX;
                    double curY = originY;
                    double curZ = originZ;

                    for (int step = 0; step < length; step++)
                    {
                        double progress = (double)step / length;
                        // Taper at start and end
                        double shape = Math.Sin(progress * Math.PI);
                        double curRadiusH = widthMultiplier * shape;
                        double curRadiusV = verticalRadius * shape;

                        if (curRadiusH > 0.8 && curRadiusV > 2.0)
                        {
                            CarveEllipsoid(chunk, targetChunkX, targetChunkZ, curX, curY, curZ, curRadiusH, curRadiusV, aquifer);
                        }

                        // Ravines follow a mostly straight or gently weaving path
                        yaw += (random.NextDouble() - 0.5) * 0.12;
                        pitch = Math.Clamp(pitch + (random.NextDouble() - 0.5) * 0.08, -0.2, 0.2);

                        double speed = 1.4;
                        curX += Math.Cos(pitch) * Math.Sin(yaw) * speed;
                        curY += Math.Sin(pitch) * speed;
                        curZ += Math.Cos(pitch) * Math.Cos(yaw) * speed;

                        if (curY < -50 || curY > 110) break;
                    }
                }
            }
        }
    }
}
