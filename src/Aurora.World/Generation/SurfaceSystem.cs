using System;

namespace Aurora.World.Generation;

/// <summary>
/// A rule that determines what block state should be placed at a specific surface coordinate.
/// </summary>
public interface ISurfaceRule
{
    ushort? Evaluate(SurfaceContext context);
}

public readonly record struct SurfaceContext(
    int X,
    int Y,
    int Z,
    BiomeType Biome,
    double Temperature,
    int SeaLevel
);

public sealed class BlockRule : ISurfaceRule
{
    private readonly ushort _state;

    public BlockRule(ushort state)
    {
        _state = state;
    }

    public ushort? Evaluate(SurfaceContext context) => _state;
}

public sealed class SequenceRule : ISurfaceRule
{
    private readonly ISurfaceRule[] _rules;

    public SequenceRule(params ISurfaceRule[] rules)
    {
        _rules = rules;
    }

    public ushort? Evaluate(SurfaceContext context)
    {
        foreach (var rule in _rules)
        {
            var result = rule.Evaluate(context);
            if (result != null) return result;
        }
        return null;
    }
}

public sealed class ConditionRule : ISurfaceRule
{
    private readonly ISurfaceCondition _condition;
    private readonly ISurfaceRule _thenRule;

    public ConditionRule(ISurfaceCondition condition, ISurfaceRule thenRule)
    {
        _condition = condition;
        _thenRule = thenRule;
    }

    public ushort? Evaluate(SurfaceContext context)
    {
        if (_condition.Test(context))
        {
            return _thenRule.Evaluate(context);
        }
        return null;
    }
}

public interface ISurfaceCondition
{
    bool Test(SurfaceContext context);
}

public sealed class BiomeCondition : ISurfaceCondition
{
    private readonly BiomeType _biome;
    public BiomeCondition(BiomeType biome) => _biome = biome;
    public bool Test(SurfaceContext context) => context.Biome == _biome;
}

public sealed class WaterDepthCondition : ISurfaceCondition
{
    private readonly int _offset;
    public WaterDepthCondition(int offset) => _offset = offset;
    public bool Test(SurfaceContext context) => context.Y <= context.SeaLevel + _offset;
}
