using Aurora.Core.Math.Noise;

namespace Aurora.World.Generation.Density;

/// <summary>
/// A density function that adds a jitter (shift) to coordinates using another noise before sampling the main noise.
/// Used to create non-linear, natural biome boundaries and terrain transitions.
/// </summary>
public sealed class ShiftedNoiseDensityFunction : IDensityFunction
{
    private readonly IDensityFunction _shiftX;
    private readonly IDensityFunction _shiftY;
    private readonly IDensityFunction _shiftZ;
    private readonly IDensityFunction _mainNoise;

    public ShiftedNoiseDensityFunction(
        IDensityFunction shiftX,
        IDensityFunction shiftY,
        IDensityFunction shiftZ,
        IDensityFunction mainNoise)
    {
        _shiftX = shiftX;
        _shiftY = shiftY;
        _shiftZ = shiftZ;
        _mainNoise = mainNoise;
    }

    public double Compute(NoiseContext context)
    {
        double dx = _shiftX.Compute(context);
        double dy = _shiftY.Compute(context);
        double dz = _shiftZ.Compute(context);
        
        // Use fractional coordinates for continuous jittering
        var shiftedContext = new NoiseContext(
            context.X + dx,
            context.Y + dy,
            context.Z + dz
        );
        
        return _mainNoise.Compute(shiftedContext);
    }

    public double MinValue => _mainNoise.MinValue;
    public double MaxValue => _mainNoise.MaxValue;
}
