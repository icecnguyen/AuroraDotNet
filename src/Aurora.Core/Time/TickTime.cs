namespace Aurora.Core.Time;

/// <summary>
/// Represents time within the simulation engine as ticks.
/// </summary>
public readonly record struct TickTime(long Ticks)
{
    public static TickTime Zero => new(0);

    public TickTime Next() => new(Ticks + 1);

    public static TickTime operator +(TickTime a, TickTime b) => new(a.Ticks + b.Ticks);
    public static TickTime operator -(TickTime a, TickTime b) => new(a.Ticks - b.Ticks);
    public TickTime Subtract(TickTime other) => this - other;
    
    public TickTime Add(long ticks) => new(Ticks + ticks);
}
