using System;
using System.Threading;

namespace Aurora.Server;

/// <summary>
/// Manages the global server tick loop and timing.
/// </summary>
public sealed class TickManager
{
    private long _currentTick;
    public long CurrentTick => Interlocked.Read(ref _currentTick);
    
    // Constant for Minecraft's TPS
    public const int TicksPerSecond = 20;
    public const int MillisecondsPerTick = 1000 / TicksPerSecond;

    public void AdvanceTick()
    {
        Interlocked.Increment(ref _currentTick);
    }
}
