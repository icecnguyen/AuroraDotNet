using Aurora.Core.Math;
using Aurora.Core.Math.Noise;
using Aurora.Core.Math.Random;

namespace Aurora.World.Generation;

public enum BiomeType : int
{
    None = 0,
    Plains = 1,
    Forest = 2,
    BirchForest = 3,
    Taiga = 4,
    SnowySlopes = 5,
    JaggedPeaks = 6,
    Desert = 7,
    Beach = 8,
    Ocean = 9,
    DeepOcean = 10,
    River = 11,
    Mountains = 12,
    Swamp = 13,
    Savanna = 14,
    Jungle = 15,
    DarkForest = 16,
    Badlands = 17
}

/// <summary>
/// Simple biome source that assigns a biome based on 2D coordinates.
/// </summary>
public sealed class BiomeSource
{
    private readonly OctavePerlinNoise _temperatureNoise;
    private readonly OctavePerlinNoise _humidityNoise;

    public BiomeSource(int seed)
    {
        var randomTemp = new PositionalRandomFactory(seed * 2).At(0, 0, 0);
        var randomHum = new PositionalRandomFactory(seed * 3).At(0, 0, 0);
        var amplitudes = new System.Collections.Generic.List<double> { 1.0, 1.0 };
        
        _temperatureNoise = new OctavePerlinNoise(randomTemp, -2, amplitudes);
        _humidityNoise = new OctavePerlinNoise(randomHum, -2, amplitudes);
    }

    public BiomeType GetBiome(int x, int z)
    {
        // Sample with a large scale so biomes are large
        double scale = 0.005;
        double temp = _temperatureNoise.GetValue(x * scale, 0, z * scale);
        double hum = _humidityNoise.GetValue(x * scale, 0, z * scale);

        // Normalize approx from -1..1 to 0..1
        temp = (temp + 1.0) / 2.0;
        hum = (hum + 1.0) / 2.0;

        if (temp > 0.6 && hum < 0.4)
            return BiomeType.Desert;
        
        if (temp < 0.4 && hum > 0.6)
            return BiomeType.Ocean;

        if (hum > 0.7)
            return BiomeType.Mountains;

        return BiomeType.Plains;
    }
}
