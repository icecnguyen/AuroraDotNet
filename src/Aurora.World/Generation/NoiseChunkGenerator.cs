using System;
using Aurora.Core.Math;
using Aurora.Core.Math.Noise;
using Aurora.Core.Math.Random;

using Aurora.World.Generation.Biome;
using Aurora.World.Generation.Density;
using Aurora.World.Generation.Features;
using Aurora.World.Generation.Structures;
using System;
using System.Collections.Generic;

namespace Aurora.World.Generation;

public sealed class NoiseChunkGenerator
{
    private readonly MultiNoiseBiomeSource _biomeSource;
    private readonly NoiseRouter _router;
    private readonly int _seed;
    private readonly List<PlacedFeature> _overworldOres;
    private readonly JigsawManager _jigsawManager;

    public NoiseChunkGenerator(int seed)
    {
        _seed = seed;
        _biomeSource = MultiNoiseBiomeSource.CreateOverworld();
        
        var randomFactory = new PositionalRandomFactory(seed);
        
        // Define base climate noises
        var tempNoise = new NoiseDensityFunction(
            new OctavePerlinNoise(randomFactory.FromHashOf("temperature").At(0,0,0), -2, new System.Collections.Generic.List<double>{1, 1}), 
            1.0, 0.0);
            
        var humNoise = new NoiseDensityFunction(
            new OctavePerlinNoise(randomFactory.FromHashOf("humidity").At(0,0,0), -2, new System.Collections.Generic.List<double>{1, 1}), 
            1.0, 0.0);
            
        var contNoise = new NoiseDensityFunction(
            new OctavePerlinNoise(randomFactory.FromHashOf("continentalness").At(0,0,0), -3, new System.Collections.Generic.List<double>{1, 1, 1}), 
            40.0, 0.0); // Amplitude 40 blocks
            
        // 3D Noise for caves and terrain detail
        var detail3D = new NoiseDensityFunction(
            new OctavePerlinNoise(randomFactory.FromHashOf("detail").At(0,0,0), -4, new System.Collections.Generic.List<double>{1, 1, 1, 1}), 
            10.0, 0.0); // Amplitude 10 blocks
            
        // Y-gradient to form ground vs sky
        // Density = Continentalness + Detail - (Y - 64) * 0.02
        var yGradient = new YGradientDensityFunction(64, 0.02);
        
        var baseDensity = new AddDensityFunction(contNoise, detail3D);
        var finalDensity = new AddDensityFunction(baseDensity, yGradient);
        
        _router = new NoiseRouter(
            tempNoise, humNoise, contNoise, tempNoise, tempNoise, tempNoise, // placeholders for erosion/depth/weirdness
            finalDensity, baseDensity, baseDensity
        );
        
        _overworldOres = new List<PlacedFeature>
        {
            // Coal Ore: Uniform between 0 and 192, 20 veins per chunk, size 17
            new PlacedFeature(new OreFeature(new OreConfiguration(1, 16, 17)), 20, new UniformHeightProvider(0, 192)),
            // Iron Ore: Triangle between -24 and 56, 10 veins per chunk, size 9
            new PlacedFeature(new OreFeature(new OreConfiguration(1, 15, 9)), 10, new TrapezoidHeightProvider(-24, 56)),
            // Diamond Ore: Triangle between -144 and 16, 7 veins per chunk, size 8 (often deep)
            new PlacedFeature(new OreFeature(new OreConfiguration(1, 56, 8)), 7, new TrapezoidHeightProvider(-144, 16, 4))
        };
        
        _jigsawManager = new JigsawManager(poolName => {
            // Mock pool resolver
            if (poolName == "minecraft:village/plains/houses")
            {
                var template = NbtTemplateReader.Load("house");
                return new JigsawPool(new List<(StructureTemplate, int)> { (template, 1) });
            }
            return null;
        });
    }

    public Chunk GenerateChunk(int chunkX, int chunkZ)
    {
        var chunk = new Chunk(new ChunkPosition(chunkX, chunkZ));
        
        int startX = chunkX * 16;
        int startZ = chunkZ * 16;

        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                int worldX = startX + x;
                int worldZ = startZ + z;

                // Evaluate climate at surface level for 2D biome mapping (like 1.18 does for surface features)
                var surfaceContext = new NoiseContext(worldX, 64, worldZ);
                var targetPoint = Climate.Target(this._router, surfaceContext);
                var biome = _biomeSource.GetBiome(targetPoint);
                
                int highestBlockY = -65;
                
                // 3D Density evaluation
                for (int y = 319; y >= -64; y--)
                {
                    var context = new NoiseContext(worldX, y, worldZ);
                    double density = _router.FinalDensity.Compute(context);
                    
                    if (density > 0)
                    {
                        chunk.SetBlockState(x, y, z, SurfaceBuilder.Stone);
                        if (highestBlockY == -65) highestBlockY = y;
                    }
                    else if (y < 63)
                    {
                        chunk.SetBlockState(x, y, z, SurfaceBuilder.Water);
                    }
                }
                
                if (highestBlockY > -65)
                {
                    SurfaceBuilder.BuildSurface(chunk, x, z, highestBlockY, biome);
                    
                    // Add trees
                    if (biome == BiomeType.Plains && chunk.GetBlockState(x, highestBlockY, z) == SurfaceBuilder.GrassBlock)
                    {
#pragma warning disable CA5394 // Random is insecure
                        var random = new Random(_seed + worldX * 31 + worldZ);
                        if (random.Next(100) < 2) // 2% chance per grass block
                        {
                            // In 1.21, Oak Log state ID is roughly 114, Oak Leaves is roughly 161 (varies, but these are safer guesses than 17/18)
                            var tree = new TreeFeature(new TreeConfiguration(114, 161)); 
                            var featureContext = new FeatureContext(chunk, x, highestBlockY + 1, z, random);
                            tree.Place(featureContext);
                        }
#pragma warning restore CA5394
                    }
                }
            }
        }
        
        // Generate features (Ores)
        var featureRandom = new Random(_seed ^ chunkX ^ (chunkZ << 16));
        var dummyContext = new NoiseContext(0, 0, 0); // Ores don't typically need real noise context for height
        
        foreach (var ore in _overworldOres)
        {
            ore.Place(chunk, dummyContext, featureRandom);
        }
        
        // Generate Structures (Jigsaw)
        // 1% chance to spawn a village-like structure at the center of the chunk
#pragma warning disable CA5394
        if (featureRandom.Next(100) == 0)
        {
            var rootTemplate = NbtTemplateReader.Load("start");
            var structures = _jigsawManager.Assemble(rootTemplate, startX + 8, 64, startZ + 8, 3, featureRandom);
            
            foreach (var placed in structures)
            {
                // Simple pasting logic
                for (int sx = 0; sx < placed.Template.SizeX; sx++)
                {
                    for (int sy = 0; sy < placed.Template.SizeY; sy++)
                    {
                        for (int sz = 0; sz < placed.Template.SizeZ; sz++)
                        {
                            int worldXPos = placed.X + sx;
                            int worldYPos = placed.Y + sy;
                            int worldZPos = placed.Z + sz;
                            
                            // Check if inside this chunk
                            if (worldXPos >= startX && worldXPos < startX + 16 &&
                                worldZPos >= startZ && worldZPos < startZ + 16)
                            {
                                ushort block = placed.Template.GetBlock(sx, sy, sz);
                                if (block != 0) // Air check
                                {
                                    chunk.SetBlockState(worldXPos - startX, worldYPos, worldZPos - startZ, block);
                                }
                            }
                        }
                    }
                }
            }
        }
#pragma warning restore CA5394

        return chunk;
    }
}
