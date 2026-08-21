using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Aurora.Core.Metrics;

/// <summary>
/// Collects basic telemetry such as TPS and memory usage.
/// </summary>
public static class MetricsRegistry
{
    private static long _totalTicks;
    private static readonly ConcurrentQueue<double> _tickTimesMs = new();
    private const int MaxSamples = 100; // Store last 100 ticks

    public static void RecordTickTime(double ms)
    {
        Interlocked.Increment(ref _totalTicks);
        _tickTimesMs.Enqueue(ms);
        
        while (_tickTimesMs.Count > MaxSamples)
        {
            _tickTimesMs.TryDequeue(out _);
        }
    }

    public static double AverageTickTimeMs
    {
        get
        {
            if (_tickTimesMs.IsEmpty) return 0.0;
            
            double sum = 0;
            int count = 0;
            foreach (var time in _tickTimesMs)
            {
                sum += time;
                count++;
            }
            
            return count == 0 ? 0 : sum / count;
        }
    }

    public static long TotalAllocatedMemoryMb => Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
}
