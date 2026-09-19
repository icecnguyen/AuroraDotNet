#pragma warning disable CA5394 // Random is insecure

using System;
using Aurora.World.Generation.Aquifers;

namespace Aurora.World.Generation.Carvers;

/// <summary>
/// Cave carver implementing random-walk tunnel carving.
/// Ported and adapted from Pumpkin-MC (pumpkin-world/src/generation/carver/cave.rs).
/// </summary>
public sealed class CaveCarver : WorldCarver
{
    private readonly int _seed;

    public CaveCarver(int seed)
    {
        _seed = seed;
    }

    public void Carve(Chunk chunk, int targetChunkX, int targetChunkZ, AquiferSampler? aquifer = null)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        // Scan neighbor chunks in radius 3 so cave branches starting nearby can tunnel into this chunk
        for (int cx = targetChunkX - 3; cx <= targetChunkX + 3; cx++)
        {
            for (int cz = targetChunkZ - 3; cz <= targetChunkZ + 3; cz++)
            {
                long chunkSeed = _seed ^ (cx * 341873128L) ^ (cz * 132897987L);
                var random = new Random((int)(chunkSeed ^ (chunkSeed >> 32)));

                // 25% chance of generating a cave network from this chunk
                if (random.Next(100) < 25)
                {
                    int branches = random.Next(1, 4);
                    for (int b = 0; b < branches; b++)
                    {
                        double originX = cx * 16 + random.Next(16);
                        double originY = random.Next(-35, 55);
                        double originZ = cz * 16 + random.Next(16);

                        int steps = random.Next(35, 75);
                        double yaw = random.NextDouble() * Math.PI * 2.0;
                        double pitch = (random.NextDouble() - 0.5) * 0.5;

                        double radiusH = 2.0 + random.NextDouble() * 1.8;
                        double radiusV = 1.8 + random.NextDouble() * 1.5;

                        double curX = originX;
                        double curY = originY;
                        double curZ = originZ;

                        for (int step = 0; step < steps; step++)
                        {
                            // Carve if nearby target chunk
                            CarveEllipsoid(chunk, targetChunkX, targetChunkZ, curX, curY, curZ, radiusH, radiusV, aquifer);

                            // Advance step
                            yaw += (random.NextDouble() - 0.5) * 0.4;
                            pitch = Math.Clamp(pitch + (random.NextDouble() - 0.5) * 0.2, -0.6, 0.6);

                            double speed = 1.2;
                            curX += Math.Cos(pitch) * Math.Sin(yaw) * speed;
                            curY += Math.Sin(pitch) * speed;
                            curZ += Math.Cos(pitch) * Math.Cos(yaw) * speed;

                            // Slight radius pulsation
                            radiusH = Math.Clamp(radiusH + (random.NextDouble() - 0.5) * 0.3, 1.8, 3.8);
                            radiusV = Math.Clamp(radiusV + (random.NextDouble() - 0.5) * 0.3, 1.5, 3.2);

                            if (curY < -58 || curY > 120) break;
                        }
                    }
                }
            }
        }
    }
}
