namespace Aurora.World.Generation.Density;

/// <summary>
/// A density function that decreases linearly with height.
/// Used to form the basic ground-to-sky gradient.
/// </summary>
public sealed class YGradientDensityFunction : IDensityFunction
{
    private readonly double _baseHeight;
    private readonly double _falloff;

    public YGradientDensityFunction(double baseHeight, double falloff)
    {
        _baseHeight = baseHeight;
        _falloff = falloff;
    }

    public double Compute(NoiseContext context)
    {
        return -((context.Y - _baseHeight) * _falloff);
    }

    public double MinValue => double.NegativeInfinity;
    public double MaxValue => double.PositiveInfinity;
}
