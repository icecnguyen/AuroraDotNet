using System;

namespace Aurora.World.Generation.Density;

public sealed class AddDensityFunction : IDensityFunction
{
    private readonly IDensityFunction _op1;
    private readonly IDensityFunction _op2;

    public AddDensityFunction(IDensityFunction op1, IDensityFunction op2)
    {
        ArgumentNullException.ThrowIfNull(op1);
        ArgumentNullException.ThrowIfNull(op2);
        _op1 = op1;
        _op2 = op2;
        MinValue = op1.MinValue + op2.MinValue;
        MaxValue = op1.MaxValue + op2.MaxValue;
    }

    public double Compute(NoiseContext context) => _op1.Compute(context) + _op2.Compute(context);
    public double MinValue { get; }
    public double MaxValue { get; }
}

public sealed class MulDensityFunction : IDensityFunction
{
    private readonly IDensityFunction _op1;
    private readonly IDensityFunction _op2;

    public MulDensityFunction(IDensityFunction op1, IDensityFunction op2)
    {
        ArgumentNullException.ThrowIfNull(op1);
        ArgumentNullException.ThrowIfNull(op2);
        _op1 = op1;
        _op2 = op2;
        
        double v1 = op1.MinValue * op2.MinValue;
        double v2 = op1.MinValue * op2.MaxValue;
        double v3 = op1.MaxValue * op2.MinValue;
        double v4 = op1.MaxValue * op2.MaxValue;
        
        MinValue = Math.Min(Math.Min(v1, v2), Math.Min(v3, v4));
        MaxValue = Math.Max(Math.Max(v1, v2), Math.Max(v3, v4));
    }

    public double Compute(NoiseContext context) => _op1.Compute(context) * _op2.Compute(context);
    public double MinValue { get; }
    public double MaxValue { get; }
}

public sealed class MinDensityFunction : IDensityFunction
{
    private readonly IDensityFunction _op1;
    private readonly IDensityFunction _op2;

    public MinDensityFunction(IDensityFunction op1, IDensityFunction op2)
    {
        ArgumentNullException.ThrowIfNull(op1);
        ArgumentNullException.ThrowIfNull(op2);
        _op1 = op1;
        _op2 = op2;
        MinValue = Math.Min(op1.MinValue, op2.MinValue);
        MaxValue = Math.Min(op1.MaxValue, op2.MaxValue);
    }

    public double Compute(NoiseContext context) => Math.Min(_op1.Compute(context), _op2.Compute(context));
    public double MinValue { get; }
    public double MaxValue { get; }
}

public sealed class MaxDensityFunction : IDensityFunction
{
    private readonly IDensityFunction _op1;
    private readonly IDensityFunction _op2;

    public MaxDensityFunction(IDensityFunction op1, IDensityFunction op2)
    {
        ArgumentNullException.ThrowIfNull(op1);
        ArgumentNullException.ThrowIfNull(op2);
        _op1 = op1;
        _op2 = op2;
        MinValue = Math.Max(op1.MinValue, op2.MinValue);
        MaxValue = Math.Max(op1.MaxValue, op2.MaxValue);
    }

    public double Compute(NoiseContext context) => Math.Max(_op1.Compute(context), _op2.Compute(context));
    public double MinValue { get; }
    public double MaxValue { get; }
}

public sealed class ConstantDensityFunction : IDensityFunction
{
    private readonly double _value;
    
    public ConstantDensityFunction(double value)
    {
        _value = value;
    }
    
    public double Compute(NoiseContext context) => _value;
    public double MinValue => _value;
    public double MaxValue => _value;
}
