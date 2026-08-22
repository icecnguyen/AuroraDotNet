using System;
using System.Collections.Generic;

namespace Aurora.World.Generation.Density;

/// <summary>
/// A cubic spline used for smooth interpolation of values like terrain height from climate parameters.
/// </summary>
public interface ICubicSpline<T>
{
    float Compute(T context);
    float MinValue { get; }
    float MaxValue { get; }
}

public sealed class ConstantSpline<T> : ICubicSpline<T>
{
    private readonly float _value;

    public ConstantSpline(float value)
    {
        _value = value;
    }

    public float Compute(T context) => _value;
    public float MinValue => _value;
    public float MaxValue => _value;
}

public sealed class MultiPointSpline<T> : ICubicSpline<T>
{
    private readonly ICoordinateExtractor<T> _coordinateExtractor;
    private readonly float[] _locations;
    private readonly ICubicSpline<T>[] _values;
    private readonly float[] _derivatives;
    private readonly float _minValue;
    private readonly float _maxValue;

    public MultiPointSpline(
        ICoordinateExtractor<T> coordinateExtractor,
        float[] locations,
        ICubicSpline<T>[] values,
        float[] derivatives)
    {
        ArgumentNullException.ThrowIfNull(values);
        _coordinateExtractor = coordinateExtractor;
        _locations = locations;
        _values = values;
        _derivatives = derivatives;
        
        float min = float.PositiveInfinity;
        float max = float.NegativeInfinity;
        
        foreach (var val in values)
        {
            if (val.MinValue < min) min = val.MinValue;
            if (val.MaxValue > max) max = val.MaxValue;
        }
        
        _minValue = min;
        _maxValue = max;
    }

    public float Compute(T context)
    {
        float coordinate = _coordinateExtractor.Extract(context);
        
        // Find interval
        int index = Array.BinarySearch(_locations, coordinate);
        if (index >= 0)
        {
            return _values[index].Compute(context);
        }
        
        index = ~index - 1;
        
        if (index < 0) return _values[0].Compute(context);
        if (index >= _locations.Length - 1) return _values[^1].Compute(context);
        
        float loc0 = _locations[index];
        float loc1 = _locations[index + 1];
        float val0 = _values[index].Compute(context);
        float val1 = _values[index + 1].Compute(context);
        float der0 = _derivatives[index];
        float der1 = _derivatives[index + 1];
        
        float t = (coordinate - loc0) / (loc1 - loc0);
        float t2 = t * t;
        float t3 = t2 * t;
        
        float h00 = 2 * t3 - 3 * t2 + 1;
        float h10 = t3 - 2 * t2 + t;
        float h01 = -2 * t3 + 3 * t2;
        float h11 = t3 - t2;
        
        // Hermite interpolation
        float result = h00 * val0 + h10 * der0 * (loc1 - loc0) + h01 * val1 + h11 * der1 * (loc1 - loc0);
        return result;
    }

    public float MinValue => _minValue;
    public float MaxValue => _maxValue;
}

public interface ICoordinateExtractor<in T>
{
    float Extract(T context);
}
