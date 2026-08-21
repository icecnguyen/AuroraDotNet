using System.Threading;

namespace Aurora.Threading;

/// <summary>
/// Tracks runtime metrics for the scheduler and workers.
/// </summary>
public sealed class SchedulerMetrics
{
    private long _completedTasks;
    private long _queuedTasks;
    
    public long CompletedTasks => Interlocked.Read(ref _completedTasks);
    public long QueuedTasks => Interlocked.Read(ref _queuedTasks);

    public void IncrementQueued() => Interlocked.Increment(ref _queuedTasks);
    
    public void RecordCompletion()
    {
        Interlocked.Decrement(ref _queuedTasks);
        Interlocked.Increment(ref _completedTasks);
    }
}
