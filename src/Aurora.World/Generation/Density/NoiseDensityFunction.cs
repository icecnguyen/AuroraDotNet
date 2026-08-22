using Aurora.Core.Math.Noise;

namespace Aurora.World.Generation.Density;

/// <summary>
/// A density function that directly samples from an OctavePerlinNoise.
/// Used for sampling base parameters like Temperature, Humidity, Continentalness, etc.
/// </summary>
public sealed class NoiseDensityFunction : IDensityFunction
{
    private readonly OctavePerlinNoise _noise;
    private readonly double _xzScale;
    private readonly double _yScale;

    public NoiseDensityFunction(OctavePerlinNoise noise, double xzScale, double yScale)
    {
        _noise = noise;
        _xzScale = xzScale;
        _yScale = yScale;
    }

    public double Compute(NoiseContext context)
    {
        // Typically, Minecraft multiplies the coordinate by the scale before passing to noise.
        return _noise.GetValue(context.X * _xzScale, context.Y * _yScale, context.Z * _xzScale);
    }

    public double MinValue => -1.0; // Approximation, usually bounded by noise amplitude
    public double MaxValue => 1.0;
}
