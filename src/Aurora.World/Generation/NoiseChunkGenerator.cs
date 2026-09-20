#pragma warning disable CA5394 // Random is insecure

using System;
using System.Buffers;
using System.Collections.Generic;
using Aurora.Core.Math;
using Aurora.Core.Math.Noise;
using Aurora.Core.Math.Random;
using Aurora.World.Generation.Biome;
using Aurora.World.Generation.Density;
using Aurora.World.Generation.Features;
using Aurora.World.Generation.Structures;
using Aurora.World.Generation.Carvers;
using Aurora.World.Generation.Aquifers;

namespace Aurora.World.Generation;

public sealed class NoiseChunkGenerator
{
    private readonly MultiNoiseBiomeSource _biomeSource;
    private readonly NoiseRouter _router;
    private readonly long _seed;
    private readonly List<PlacedFeature> _overworldOres;
    private readonly JigsawManager _jigsawManager;
    private readonly CaveCarver _caveCarver;
    private readonly CanyonCarver _canyonCarver;
    private readonly AquiferSampler _aquiferSampler;

    private readonly NoiseDensityFunction _terrainDetail;
    private readonly NoiseDensityFunction _caveNoise3D;

    private const int CellSizeX = 4;
    private const int CellSizeZ = 4;
    private const int CellSizeY = 8;
    private const int GridX = 16 / CellSizeX + 1; // 5
    private const int GridZ = 16 / CellSizeZ + 1; // 5
    private const int GridY = 384 / CellSizeY + 1; // 49 (-64 to 320)

    public NoiseChunkGenerator(long seed)
    {
        _seed = seed;
        _biomeSource = MultiNoiseBiomeSource.CreateOverworld();
        
        var randomFactory = new PositionalRandomFactory(seed);
        
        // Base climate noises with natural scales (low frequency for broad biomes)
        var tempNoise = new NoiseDensityFunction(
            new OctavePerlinNoise(randomFactory.FromHashOf("temperature").At(0, 0, 0), -2, new List<double> { 1, 1 }), 
            0.002, 0.0);
            
        var humNoise = new NoiseDensityFunction(
            new OctavePerlinNoise(randomFactory.FromHashOf("humidity").At(0, 0, 0), -2, new List<double> { 1, 1 }), 
            0.002, 0.0);
            
        var contNoise = new NoiseDensityFunction(
            new OctavePerlinNoise(randomFactory.FromHashOf("continentalness").At(0, 0, 0), -3, new List<double> { 1, 1, 1 }), 
            0.003, 0.0);

        var erosionNoise = new NoiseDensityFunction(
            new OctavePerlinNoise(randomFactory.FromHashOf("erosion").At(0, 0, 0), -3, new List<double> { 1, 1, 1 }), 
            0.004, 0.0);

        var depthNoise = new NoiseDensityFunction(
            new OctavePerlinNoise(randomFactory.FromHashOf("depth").At(0, 0, 0), -2, new List<double> { 1, 1 }), 
            0.01, 0.0);

        var weirdnessNoise = new NoiseDensityFunction(
            new OctavePerlinNoise(randomFactory.FromHashOf("weirdness").At(0, 0, 0), -3, new List<double> { 1, 1, 1 }), 
            0.005, 0.0);

        // Terrain detail 2D noise for rolling hills and natural surface variation
        _terrainDetail = new NoiseDensityFunction(
            new OctavePerlinNoise(randomFactory.FromHashOf("terrain_detail").At(0, 0, 0), -3, new List<double> { 1, 1, 1 }), 
            0.012, 0.0);

        // 3D noise for caves and underground hollows
        _caveNoise3D = new NoiseDensityFunction(
            new OctavePerlinNoise(randomFactory.FromHashOf("caves_3d").At(0, 0, 0), -3, new List<double> { 1, 1, 1 }), 
            0.025, 0.035);

        var dummyDensity = new YGradientDensityFunction(64, 0.02);

        _router = new NoiseRouter(
            tempNoise, humNoise, contNoise, erosionNoise, depthNoise, weirdnessNoise,
            dummyDensity, dummyDensity, dummyDensity
        );
        
        // Standard vanilla ore configurations matching Minecraft 1.21.4 block IDs
        _overworldOres = new List<PlacedFeature>
        {
            // Coal Ore: Stone layer (0 to 192), 20 veins per chunk, size 17
            new PlacedFeature(new OreFeature(new OreConfiguration(Block.Stone, Block.CoalOre, 17)), 20, new UniformHeightProvider(0, 192)),
            // Iron Ore: Upper Stone (-24 to 56), 12 veins per chunk, size 9
            new PlacedFeature(new OreFeature(new OreConfiguration(Block.Stone, Block.IronOre, 9)), 12, new TrapezoidHeightProvider(-24, 56)),
            // Iron Ore: Deepslate (-64 to 0), 8 veins per chunk, size 9
            new PlacedFeature(new OreFeature(new OreConfiguration(Block.Deepslate, Block.DeepslateIronOre, 9)), 8, new TrapezoidHeightProvider(-64, 0)),
            // Copper Ore: Stone (-16 to 112), 12 veins per chunk, size 10
            new PlacedFeature(new OreFeature(new OreConfiguration(Block.Stone, Block.CopperOre, 10)), 12, new TrapezoidHeightProvider(-16, 112)),
            // Gold Ore: Deepslate (-64 to 0), 6 veins per chunk, size 9
            new PlacedFeature(new OreFeature(new OreConfiguration(Block.Deepslate, Block.DeepslateGoldOre, 9)), 6, new TrapezoidHeightProvider(-64, 0)),
            // Redstone Ore: Deepslate (-64 to 15), 8 veins per chunk, size 8
            new PlacedFeature(new OreFeature(new OreConfiguration(Block.Deepslate, Block.DeepslateRedstoneOre, 8)), 8, new TrapezoidHeightProvider(-64, 15)),
            // Diamond Ore: Deepslate (-64 to 0), 7 veins per chunk, size 8 (more frequent at bottom)
            new PlacedFeature(new OreFeature(new OreConfiguration(Block.Deepslate, Block.DeepslateDiamondOre, 8)), 7, new TrapezoidHeightProvider(-144, 16, 4)),
            // Lapis Ore: Stone & Deepslate (-32 to 32), 3 veins per chunk, size 7
            new PlacedFeature(new OreFeature(new OreConfiguration(Block.Stone, Block.LapisOre, 7)), 3, new TrapezoidHeightProvider(-32, 32)),

            // Geologic Stone & Deepslate Blobs
            new PlacedFeature(new OreFeature(new OreConfiguration(Block.Stone, Block.Granite, 48)), 6, new UniformHeightProvider(0, 128)),
            new PlacedFeature(new OreFeature(new OreConfiguration(Block.Stone, Block.Diorite, 48)), 6, new UniformHeightProvider(0, 128)),
            new PlacedFeature(new OreFeature(new OreConfiguration(Block.Stone, Block.Andesite, 48)), 6, new UniformHeightProvider(0, 128)),
            new PlacedFeature(new OreFeature(new OreConfiguration(Block.Deepslate, Block.Tuff, 48)), 8, new UniformHeightProvider(-64, 0))
        };

        _caveCarver = new CaveCarver(seed);
        _canyonCarver = new CanyonCarver(seed);
        _aquiferSampler = new AquiferSampler(seed);
        
        _jigsawManager = new JigsawManager(poolName => {
            if (poolName == "minecraft:village/plains/houses")
            {
                var template = NbtTemplateReader.Load("house");
                return new JigsawPool(new List<(StructureTemplate, int)> { (template, 1) });
            }
            return null;
        });
    }

    public double ComputeBaseHeight(int worldX, int worldZ)
    {
        var context2D = new NoiseContext(worldX, 64, worldZ);
        double cont = _router.Continentalness.Compute(context2D);
        double erosion = _router.Erosion.Compute(context2D);
        double detail = _terrainDetail.Compute(context2D);

        double spawnDist = Math.Sqrt(worldX * (double)worldX + worldZ * (double)worldZ);
        double spawnLandBias = Math.Max(0.0, 1.0 - spawnDist / 384.0) * 10.0;

        if (cont < -0.45)
        {
            // Deep Ocean: trench down to Y = 32 - 42
            return 36.0 + (cont + 0.45) * 16.0 + detail * 4.0;
        }
        else if (cont < -0.15)
        {
            // Shallow Ocean / Shelf: Y = 46 - 58
            double t = (cont + 0.45) / 0.30;
            return 44.0 + t * 14.0 + detail * 3.0;
        }
        else if (cont < -0.02)
        {
            // Coastline / Beach: Y = 60 - 65
            double t = (cont + 0.15) / 0.13;
            return 59.0 + t * 5.0 + detail * 2.0;
        }
        else
        {
            // Inland: Plains, Forests, Mountains
            if (erosion < -0.45)
            {
                // Jagged Peaks / High Mountains soaring up to Y = 110 - 150!
                double peakIntensity = Math.Abs(erosion + 0.45) * 2.0;
                return 82.0 + spawnLandBias + cont * 26.0 + peakIntensity * 45.0 + detail * 8.0;
            }
            else
            {
                // Rolling hills, plains, forests: Y = 66 - 80
                return 69.0 + spawnLandBias + cont * 16.0 - erosion * 5.0 + detail * 4.0;
            }
        }
    }

    public int GetApproxGroundY(int worldX, int worldZ)
    {
        return (int)Math.Round(ComputeBaseHeight(worldX, worldZ));
    }

    public Chunk GenerateChunk(int chunkX, int chunkZ)
    {
        var chunk = new Chunk(new ChunkPosition(chunkX, chunkZ));
        
        int startX = chunkX * 16;
        int startZ = chunkZ * 16;

        // 1. Rent buffers from ArrayPool to eliminate GC allocation
        double[] densityGrid = ArrayPool<double>.Shared.Rent(GridX * GridY * GridZ);
        int[] highestSolidY = ArrayPool<int>.Shared.Rent(256);
        Array.Fill(highestSolidY, -65, 0, 256);

        try
        {
            // Compute density at 5x49x5 sample grid (Trilinear Interpolation base)
            for (int gx = 0; gx < GridX; gx++)
            {
                int sampleX = startX + gx * CellSizeX;
                for (int gz = 0; gz < GridZ; gz++)
                {
                    int sampleZ = startZ + gz * CellSizeZ;
                    double baseHeight = ComputeBaseHeight(sampleX, sampleZ);

                    for (int gy = 0; gy < GridY; gy++)
                    {
                        int sampleY = -64 + gy * CellSizeY;
                        
                        // Vertical falloff: positive under ground, negative in air
                        double density = (baseHeight - sampleY) * 0.25;

                        // 3D Cave carving: carve smooth tunnels/hollows, NEVER add positive lumps!
                        if (sampleY < baseHeight - 6 && sampleY > -54)
                        {
                            double cave = _caveNoise3D.Compute(new NoiseContext(sampleX, sampleY, sampleZ));
                            if (Math.Abs(cave) < 0.12)
                            {
                                density -= 5.0; // Carve hollow cave
                            }
                        }

                        // Force solid bedrock floor
                        if (sampleY <= -60)
                        {
                            density = Math.Max(density, 10.0);
                        }

                        densityGrid[(gx * GridY + gy) * GridZ + gz] = density;
                    }
                }
            }

            // 2. Interpolate blocks using fast Trilinear Interpolation
            for (int cx = 0; cx < 4; cx++)
            {
                for (int cz = 0; cz < 4; cz++)
                {
                    for (int cy = 0; cy < 48; cy++)
                    {
                        double d000 = densityGrid[(cx * GridY + cy) * GridZ + cz];
                        double d100 = densityGrid[((cx + 1) * GridY + cy) * GridZ + cz];
                        double d010 = densityGrid[(cx * GridY + (cy + 1)) * GridZ + cz];
                        double d110 = densityGrid[((cx + 1) * GridY + (cy + 1)) * GridZ + cz];
                        double d001 = densityGrid[(cx * GridY + cy) * GridZ + (cz + 1)];
                        double d101 = densityGrid[((cx + 1) * GridY + cy) * GridZ + (cz + 1)];
                        double d011 = densityGrid[(cx * GridY + (cy + 1)) * GridZ + (cz + 1)];
                        double d111 = densityGrid[((cx + 1) * GridY + (cy + 1)) * GridZ + (cz + 1)];

                        for (int subY = 0; subY < CellSizeY; subY++)
                        {
                            double fy = (double)subY / CellSizeY;
                            int blockY = -64 + cy * CellSizeY + subY;

                            for (int subZ = 0; subZ < CellSizeZ; subZ++)
                            {
                                double fz = (double)subZ / CellSizeZ;
                                int blockZ = cz * CellSizeZ + subZ;

                                for (int subX = 0; subX < CellSizeX; subX++)
                                {
                                    double fx = (double)subX / CellSizeX;
                                    int blockX = cx * CellSizeX + subX;

                                    // Trilinear interpolation
                                    double d00 = d000 + fx * (d100 - d000);
                                    double d01 = d001 + fx * (d101 - d001);
                                    double d10 = d010 + fx * (d110 - d010);
                                    double d11 = d011 + fx * (d111 - d011);

                                    double d0 = d00 + fz * (d01 - d00);
                                    double d1 = d10 + fz * (d11 - d10);

                                    double density = d0 + fy * (d1 - d0);

                                    if (density > 0)
                                    {
                                        chunk.SetBlockState(blockX, blockY, blockZ, Block.Stone);
                                        int colIdx = blockX * 16 + blockZ;
                                        if (blockY > highestSolidY[colIdx])
                                        {
                                            highestSolidY[colIdx] = blockY;
                                        }
                                    }
                                    else if (blockY <= -54 && blockY > -60)
                                    {
                                        // Deep subterranean Lava Lakes
                                        chunk.SetBlockState(blockX, blockY, blockZ, Block.Lava);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 2.5 Carver Pass: Cave Worms & Canyon Ravines with Aquifers
            _caveCarver.Carve(chunk, chunkX, chunkZ, _aquiferSampler);
            _canyonCarver.Carve(chunk, chunkX, chunkZ, _aquiferSampler);

            // Recompute highestSolidY after carving
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int col = x * 16 + z;
                    for (int y = highestSolidY[col]; y >= -64; y--)
                    {
                        ushort b = chunk.GetBlockState(x, y, z);
                        if (b != Block.Air && b != Block.Water && b != Block.Lava)
                        {
                            highestSolidY[col] = y;
                            break;
                        }
                    }
                }
            }

            // 3. Surface & Biome Layering pass
            var random = new Random((int)(_seed ^ (chunkX * 341873128L) ^ (chunkZ * 132897987L)));
            BiomeType centerBiome = BiomeType.Plains;

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int worldX = startX + x;
                    int worldZ = startZ + z;

                    var surfaceContext = new NoiseContext(worldX, 64, worldZ);
                    var targetPoint = Climate.Target(_router, surfaceContext);
                    var biome = _biomeSource.GetBiome(targetPoint);
                    if (x == 8 && z == 8) centerBiome = biome;

                    int highY = highestSolidY[x * 16 + z];
                    SurfaceBuilder.BuildSurface(chunk, x, z, highY, biome, random);

                    // Biome-specific vegetation & flora pass
                    if (highY >= 63)
                    {
                        ushort surfaceBlock = chunk.GetBlockState(x, highY, z);
                        int r = random.Next(1000);

                        // A. Grassland / Woodland Flora
                        if (surfaceBlock == Block.GrassBlock)
                        {
                            if (r < 80) // 8% short grass
                            {
                                if (chunk.GetBlockState(x, highY + 1, z) == Block.Air)
                                    chunk.SetBlockState(x, highY + 1, z, Block.ShortGrass);
                            }
                            else if (r < 94) // 1.4% dandelion
                            {
                                if (chunk.GetBlockState(x, highY + 1, z) == Block.Air)
                                    chunk.SetBlockState(x, highY + 1, z, Block.Dandelion);
                            }
                            else if (r < 108) // 1.4% poppy
                            {
                                if (chunk.GetBlockState(x, highY + 1, z) == Block.Air)
                                    chunk.SetBlockState(x, highY + 1, z, Block.Poppy);
                            }
                            else if (r < 118) // 1.0% cornflower
                            {
                                if (chunk.GetBlockState(x, highY + 1, z) == Block.Air)
                                    chunk.SetBlockState(x, highY + 1, z, Block.Cornflower);
                            }
                            else if (r < 126) // 0.8% allium
                            {
                                if (chunk.GetBlockState(x, highY + 1, z) == Block.Air)
                                    chunk.SetBlockState(x, highY + 1, z, Block.Allium);
                            }
                        }
                        // B. Taiga Flora (Fern on Podzol or Grass)
                        else if ((surfaceBlock == Block.Podzol || surfaceBlock == Block.GrassBlock) && biome == BiomeType.Taiga)
                        {
                            if (r < 100 && chunk.GetBlockState(x, highY + 1, z) == Block.Air)
                            {
                                chunk.SetBlockState(x, highY + 1, z, Block.Fern);
                            }
                        }
                        // C. Desert Flora (Dead Bush & Cactus on Sand)
                        else if (surfaceBlock == Block.Sand && biome == BiomeType.Desert)
                        {
                            if (r < 15 && chunk.GetBlockState(x, highY + 1, z) == Block.Air)
                            {
                                chunk.SetBlockState(x, highY + 1, z, Block.DeadBush);
                            }
                            else if (r < 23 && highY + 3 <= 250) // Cactus
                            {
                                // Cactus requires surrounding air
                                bool airSurround = true;
                                if (x > 0 && chunk.GetBlockState(x - 1, highY + 1, z) != Block.Air) airSurround = false;
                                if (x < 15 && chunk.GetBlockState(x + 1, highY + 1, z) != Block.Air) airSurround = false;
                                if (z > 0 && chunk.GetBlockState(x, highY + 1, z - 1) != Block.Air) airSurround = false;
                                if (z < 15 && chunk.GetBlockState(x, highY + 1, z + 1) != Block.Air) airSurround = false;

                                if (airSurround && chunk.GetBlockState(x, highY + 1, z) == Block.Air)
                                {
                                    int cactusHeight = random.Next(2, 4);
                                    for (int cy = 1; cy <= cactusHeight; cy++)
                                        chunk.SetBlockState(x, highY + cy, z, Block.Cactus);
                                }
                            }
                        }
                        // D. Swamp Flora (Blue Orchid)
                        else if ((surfaceBlock == Block.GrassBlock || surfaceBlock == Block.Mud) && biome == BiomeType.Swamp)
                        {
                            if (r < 75 && chunk.GetBlockState(x, highY + 1, z) == Block.Air)
                            {
                                chunk.SetBlockState(x, highY + 1, z, Block.BlueOrchid);
                            }
                        }
                        // E. Badlands Flora (Dead Bush on Terracotta / Red Sand)
                        else if ((surfaceBlock == Block.Terracotta || surfaceBlock == Block.RedSand) && biome == BiomeType.Badlands)
                        {
                            if (r < 18 && chunk.GetBlockState(x, highY + 1, z) == Block.Air)
                            {
                                chunk.SetBlockState(x, highY + 1, z, Block.DeadBush);
                            }
                        }
                    }
                }
            }

            // 4. Biome-Aware Seamless Tree Generation (Cross-Chunk Margin Overlap)
            // Trees only grow in valid biomes and STRICTLY on GrassBlock, Dirt, or Podzol!
            // ABSOLUTELY NEVER on Sand, Gravel, Stone, Water, or Air!
            for (int wx = startX - 2; wx <= startX + 17; wx++)
            {
                for (int wz = startZ - 2; wz <= startZ + 17; wz++)
                {
                    var targetPoint = Climate.Target(_router, new NoiseContext(wx, 64, wz));
                    var treeBiome = _biomeSource.GetBiome(targetPoint);

                    // Tree density based on biome
                    int treeChancePerThousand = treeBiome switch
                    {
                        BiomeType.Forest => 45,       // Dense forest
                        BiomeType.BirchForest => 40,  // Birch woodland
                        BiomeType.Taiga => 35,        // Coniferous taiga
                        BiomeType.Plains => 8,        // Sparse plains trees
                        BiomeType.Savanna => 16,      // Acacia trees
                        BiomeType.Jungle => 55,       // High density rainforest
                        BiomeType.DarkForest => 60,   // Ultra-dense dark oak roof
                        BiomeType.Swamp => 22,        // Swamp oaks with vines
                        _ => 0                        // 0 in desert, beach, ocean, badlands, snowy peaks!
                    };

                    if (treeChancePerThousand == 0) continue;

                    uint th = (uint)(wx * 374761393L ^ wz * 668265263L ^ _seed);
                    th ^= th >> 13;
                    th *= 1274126177;
                    th ^= th >> 16;

                    if ((th % 1000) >= treeChancePerThousand) continue;

                    int lx = wx - startX;
                    int lz = wz - startZ;
                    int groundY;

                    if (lx >= 0 && lx < 16 && lz >= 0 && lz < 16)
                    {
                        groundY = highestSolidY[lx * 16 + lz];
                        ushort groundBlock = chunk.GetBlockState(lx, groundY, lz);

                        // Strict soil validation: MUST be GrassBlock, Dirt, or Podzol!
                        // NEVER plant a tree on Sand, Gravel, Stone, Water!
                        if (groundBlock != Block.GrassBlock && groundBlock != Block.Dirt && groundBlock != Block.Podzol)
                            continue;
                    }
                    else
                    {
                        groundY = GetApproxGroundY(wx, wz);
                        if (groundY < 64) continue; // Outside chunk must be above sea level
                    }

                    if (groundY < 63 || groundY > 120) continue;

                    // Place tree according to biome
                    if (treeBiome == BiomeType.Taiga)
                    {
                        GenerateSpruceTree(chunk, lx, groundY, lz, th);
                    }
                    else if (treeBiome == BiomeType.BirchForest)
                    {
                        GenerateBirchTree(chunk, lx, groundY, lz, th);
                    }
                    else if (treeBiome == BiomeType.Savanna)
                    {
                        GenerateAcaciaTree(chunk, lx, groundY, lz, th);
                    }
                    else if (treeBiome == BiomeType.Jungle)
                    {
                        GenerateJungleTree(chunk, lx, groundY, lz, th);
                    }
                    else if (treeBiome == BiomeType.DarkForest)
                    {
                        GenerateDarkOakTree(chunk, lx, groundY, lz, th);
                    }
                    else if (treeBiome == BiomeType.Swamp)
                    {
                        GenerateSwampTree(chunk, lx, groundY, lz, th);
                    }
                    else
                    {
                        GenerateOakTree(chunk, lx, groundY, lz, th);
                    }
                }
            }

            // 5. Generate features (Ores)
            var featureRandom = new Random((int)(_seed ^ chunkX ^ ((long)chunkZ << 16)));
            var dummyContext = new NoiseContext(0, 0, 0);

            foreach (var ore in _overworldOres)
            {
                ore.Place(chunk, dummyContext, featureRandom);
            }

            // 6. Generate Authentic Structures (Ruined Nether Portal, Village House, Desert Well, Campsite)
            Aurora.World.Generation.Structures.StructurePlacer.PlaceStructures(chunk, chunkX, chunkZ, highestSolidY, centerBiome, _seed);

            return chunk;
        }
        finally
        {
            ArrayPool<double>.Shared.Return(densityGrid);
            ArrayPool<int>.Shared.Return(highestSolidY);
        }
    }

    private static void GenerateOakTree(Chunk chunk, int lx, int groundY, int lz, uint hash)
    {
        int treeHeight = 5 + (int)((hash >> 8) % 3); // 5, 6, or 7 blocks

        // Trunk
        for (int ty = 1; ty <= treeHeight; ty++)
        {
            int trunkY = groundY + ty;
            if (lx >= 0 && lx < 16 && lz >= 0 && lz < 16)
            {
                chunk.SetBlockState(lx, trunkY, lz, Block.OakLog);
            }
        }
        if (lx >= 0 && lx < 16 && lz >= 0 && lz < 16)
        {
            chunk.SetBlockState(lx, groundY, lz, Block.Dirt);
        }

        // Leaves canopy
        for (int dy = treeHeight - 2; dy <= treeHeight + 1; dy++)
        {
            int ly = groundY + dy;
            int rad = (dy >= treeHeight) ? 1 : 2;

            for (int dx = -rad; dx <= rad; dx++)
            {
                for (int dz = -rad; dz <= rad; dz++)
                {
                    if (Math.Abs(dx) == rad && Math.Abs(dz) == rad && (rad > 1 || dy == treeHeight + 1))
                        continue;

                    int leafX = lx + dx;
                    int leafZ = lz + dz;

                    if (leafX >= 0 && leafX < 16 && leafZ >= 0 && leafZ < 16)
                    {
                        if (chunk.GetBlockState(leafX, ly, leafZ) == Block.Air)
                        {
                            chunk.SetBlockState(leafX, ly, leafZ, Block.OakLeaves);
                        }
                    }
                }
            }
        }
    }

    private static void GenerateBirchTree(Chunk chunk, int lx, int groundY, int lz, uint hash)
    {
        int treeHeight = 5 + (int)((hash >> 8) % 3);

        // Trunk
        for (int ty = 1; ty <= treeHeight; ty++)
        {
            int trunkY = groundY + ty;
            if (lx >= 0 && lx < 16 && lz >= 0 && lz < 16)
            {
                chunk.SetBlockState(lx, trunkY, lz, Block.BirchLog);
            }
        }
        if (lx >= 0 && lx < 16 && lz >= 0 && lz < 16)
        {
            chunk.SetBlockState(lx, groundY, lz, Block.Dirt);
        }

        // Leaves canopy
        for (int dy = treeHeight - 2; dy <= treeHeight + 1; dy++)
        {
            int ly = groundY + dy;
            int rad = (dy >= treeHeight) ? 1 : 2;

            for (int dx = -rad; dx <= rad; dx++)
            {
                for (int dz = -rad; dz <= rad; dz++)
                {
                    if (Math.Abs(dx) == rad && Math.Abs(dz) == rad && (rad > 1 || dy == treeHeight + 1))
                        continue;

                    int leafX = lx + dx;
                    int leafZ = lz + dz;

                    if (leafX >= 0 && leafX < 16 && leafZ >= 0 && leafZ < 16)
                    {
                        if (chunk.GetBlockState(leafX, ly, leafZ) == Block.Air)
                        {
                            chunk.SetBlockState(leafX, ly, leafZ, Block.BirchLeaves);
                        }
                    }
                }
            }
        }
    }

    private static void GenerateSpruceTree(Chunk chunk, int lx, int groundY, int lz, uint hash)
    {
        int treeHeight = 7 + (int)((hash >> 8) % 3); // 7, 8, 9 blocks tall

        // Trunk
        for (int ty = 1; ty <= treeHeight; ty++)
        {
            int trunkY = groundY + ty;
            if (lx >= 0 && lx < 16 && lz >= 0 && lz < 16)
            {
                chunk.SetBlockState(lx, trunkY, lz, Block.SpruceLog);
            }
        }
        if (lx >= 0 && lx < 16 && lz >= 0 && lz < 16)
        {
            chunk.SetBlockState(lx, groundY, lz, Block.Dirt);
        }

        // Top spike of foliage
        int topY = groundY + treeHeight + 1;
        if (lx >= 0 && lx < 16 && lz >= 0 && lz < 16 && chunk.GetBlockState(lx, topY, lz) == Block.Air)
            chunk.SetBlockState(lx, topY, lz, Block.SpruceLeaves);

        // Stepped conical foliage
        for (int dy = treeHeight; dy >= 2; dy--)
        {
            int ly = groundY + dy;
            int rad = ((treeHeight - dy) % 2 == 0) ? 1 : 2;
            if (dy <= 3) rad = 2;

            for (int dx = -rad; dx <= rad; dx++)
            {
                for (int dz = -rad; dz <= rad; dz++)
                {
                    if (Math.Abs(dx) == rad && Math.Abs(dz) == rad && rad > 1)
                        continue;

                    int leafX = lx + dx;
                    int leafZ = lz + dz;

                    if (leafX >= 0 && leafX < 16 && leafZ >= 0 && leafZ < 16)
                    {
                        if (chunk.GetBlockState(leafX, ly, leafZ) == Block.Air)
                        {
                            chunk.SetBlockState(leafX, ly, leafZ, Block.SpruceLeaves);
                        }
                    }
                }
            }
        }
    }

    private static void GenerateAcaciaTree(Chunk chunk, int lx, int groundY, int lz, uint hash)
    {
        int treeHeight = 6 + (int)((hash >> 8) % 3); // 6, 7, 8 blocks tall
        int dirX = (int)((hash >> 4) % 3) - 1;
        int dirZ = (int)((hash >> 6) % 3) - 1;
        if (dirX == 0 && dirZ == 0) dirX = 1;

        int curX = lx;
        int curZ = lz;

        // Angled Trunk
        for (int ty = 1; ty <= treeHeight; ty++)
        {
            int trunkY = groundY + ty;
            if (ty >= 3 && ty % 2 == 1)
            {
                curX += dirX;
                curZ += dirZ;
            }

            if (curX >= 0 && curX < 16 && curZ >= 0 && curZ < 16)
            {
                chunk.SetBlockState(curX, trunkY, curZ, Block.AcaciaLog);
            }
        }
        if (lx >= 0 && lx < 16 && lz >= 0 && lz < 16)
        {
            chunk.SetBlockState(lx, groundY, lz, Block.Dirt);
        }

        // Flat umbrella parasol canopy at top (Radius 3)
        int canopyY = groundY + treeHeight;
        for (int dx = -3; dx <= 3; dx++)
        {
            for (int dz = -3; dz <= 3; dz++)
            {
                if (Math.Abs(dx) == 3 && Math.Abs(dz) == 3) continue;

                int leafX = curX + dx;
                int leafZ = curZ + dz;
                if (leafX >= 0 && leafX < 16 && leafZ >= 0 && leafZ < 16)
                {
                    if (chunk.GetBlockState(leafX, canopyY, leafZ) == Block.Air)
                        chunk.SetBlockState(leafX, canopyY, leafZ, Block.AcaciaLeaves);
                }
            }
        }

        // 3x3 cap on top of umbrella
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                int leafX = curX + dx;
                int leafZ = curZ + dz;
                if (leafX >= 0 && leafX < 16 && leafZ >= 0 && leafZ < 16)
                {
                    if (chunk.GetBlockState(leafX, canopyY + 1, leafZ) == Block.Air)
                        chunk.SetBlockState(leafX, canopyY + 1, leafZ, Block.AcaciaLeaves);
                }
            }
        }
    }

    private static void GenerateJungleTree(Chunk chunk, int lx, int groundY, int lz, uint hash)
    {
        int treeHeight = 11 + (int)((hash >> 8) % 6); // 11 to 16 blocks tall

        // Tall straight Trunk
        for (int ty = 1; ty <= treeHeight; ty++)
        {
            int trunkY = groundY + ty;
            if (lx >= 0 && lx < 16 && lz >= 0 && lz < 16)
            {
                chunk.SetBlockState(lx, trunkY, lz, Block.JungleLog);

                // Hanging vines along the trunk
                if (lx > 0 && ((hash + ty) % 3 == 0) && chunk.GetBlockState(lx - 1, trunkY, lz) == Block.Air)
                    chunk.SetBlockState(lx - 1, trunkY, lz, Block.Vine);
                if (lx < 15 && ((hash + ty * 2) % 3 == 0) && chunk.GetBlockState(lx + 1, trunkY, lz) == Block.Air)
                    chunk.SetBlockState(lx + 1, trunkY, lz, Block.Vine);
            }
        }
        if (lx >= 0 && lx < 16 && lz >= 0 && lz < 16)
        {
            chunk.SetBlockState(lx, groundY, lz, Block.Dirt);
        }

        // Bushy jungle canopy
        for (int dy = treeHeight - 2; dy <= treeHeight + 2; dy++)
        {
            int ly = groundY + dy;
            int rad = (dy >= treeHeight + 1) ? 1 : 2;

            for (int dx = -rad; dx <= rad; dx++)
            {
                for (int dz = -rad; dz <= rad; dz++)
                {
                    if (Math.Abs(dx) == rad && Math.Abs(dz) == rad && (rad > 1 || dy == treeHeight + 2))
                        continue;

                    int leafX = lx + dx;
                    int leafZ = lz + dz;

                    if (leafX >= 0 && leafX < 16 && leafZ >= 0 && leafZ < 16)
                    {
                        if (chunk.GetBlockState(leafX, ly, leafZ) == Block.Air)
                        {
                            chunk.SetBlockState(leafX, ly, leafZ, Block.JungleLeaves);
                        }
                    }
                }
            }
        }
    }

    private static void GenerateDarkOakTree(Chunk chunk, int lx, int groundY, int lz, uint hash)
    {
        int treeHeight = 6 + (int)((hash >> 8) % 3); // 6 to 8 blocks tall

        // 2x2 Thick Trunk
        for (int ty = 1; ty <= treeHeight; ty++)
        {
            int trunkY = groundY + ty;
            for (int bx = 0; bx <= 1; bx++)
            {
                for (int bz = 0; bz <= 1; bz++)
                {
                    int tx = lx + bx;
                    int tz = lz + bz;
                    if (tx >= 0 && tx < 16 && tz >= 0 && tz < 16)
                    {
                        chunk.SetBlockState(tx, trunkY, tz, Block.DarkOakLog);
                    }
                }
            }
        }
        for (int bx = 0; bx <= 1; bx++)
        {
            for (int bz = 0; bz <= 1; bz++)
            {
                int tx = lx + bx;
                int tz = lz + bz;
                if (tx >= 0 && tx < 16 && tz >= 0 && tz < 16)
                {
                    chunk.SetBlockState(tx, groundY, tz, Block.Dirt);
                }
            }
        }

        // Massive thick leaf roof (5x5 and 7x7)
        int canopyY = groundY + treeHeight;
        for (int dy = -1; dy <= 1; dy++)
        {
            int ly = canopyY + dy;
            int rad = (dy == 1) ? 2 : 3;

            for (int dx = -rad; dx <= rad + 1; dx++)
            {
                for (int dz = -rad; dz <= rad + 1; dz++)
                {
                    int leafX = lx + dx;
                    int leafZ = lz + dz;

                    if (leafX >= 0 && leafX < 16 && leafZ >= 0 && leafZ < 16)
                    {
                        if (chunk.GetBlockState(leafX, ly, leafZ) == Block.Air)
                        {
                            chunk.SetBlockState(leafX, ly, leafZ, Block.DarkOakLeaves);
                        }
                    }
                }
            }
        }
    }

    private static void GenerateSwampTree(Chunk chunk, int lx, int groundY, int lz, uint hash)
    {
        int treeHeight = 5 + (int)((hash >> 8) % 3); // 5 to 7 blocks tall

        // Trunk
        for (int ty = 1; ty <= treeHeight; ty++)
        {
            int trunkY = groundY + ty;
            if (lx >= 0 && lx < 16 && lz >= 0 && lz < 16)
            {
                chunk.SetBlockState(lx, trunkY, lz, Block.OakLog);
            }
        }
        if (lx >= 0 && lx < 16 && lz >= 0 && lz < 16)
        {
            chunk.SetBlockState(lx, groundY, lz, Block.Dirt);
        }

        // Canopy with drooping vines
        for (int dy = treeHeight - 1; dy <= treeHeight + 1; dy++)
        {
            int ly = groundY + dy;
            int rad = (dy == treeHeight + 1) ? 1 : 3;

            for (int dx = -rad; dx <= rad; dx++)
            {
                for (int dz = -rad; dz <= rad; dz++)
                {
                    if (Math.Abs(dx) == rad && Math.Abs(dz) == rad && rad > 1) continue;

                    int leafX = lx + dx;
                    int leafZ = lz + dz;

                    if (leafX >= 0 && leafX < 16 && leafZ >= 0 && leafZ < 16)
                    {
                        if (chunk.GetBlockState(leafX, ly, leafZ) == Block.Air)
                        {
                            chunk.SetBlockState(leafX, ly, leafZ, Block.OakLeaves);

                            // Drooping vines on outer leaves
                            if (dy == treeHeight - 1 && (Math.Abs(dx) == rad || Math.Abs(dz) == rad))
                            {
                                int vineLen = 1 + (int)((hash + dx + dz) % 3);
                                for (int vy = 1; vy <= vineLen; vy++)
                                {
                                    int vineY = ly - vy;
                                    if (vineY > groundY && chunk.GetBlockState(leafX, vineY, leafZ) == Block.Air)
                                    {
                                        chunk.SetBlockState(leafX, vineY, leafZ, Block.Vine);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

