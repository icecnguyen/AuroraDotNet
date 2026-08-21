using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Aurora.Threading;

/// <summary>
/// A scheduler for executing tasks on a specific tick in the future, or on a repeating interval.
/// </summary>
public sealed class GameScheduler
{
    private readonly ConcurrentDictionary<long, ConcurrentQueue<IWorkItem>> _scheduledTasks = new();
    private long _currentTick;

    public long CurrentTick => Interlocked.Read(ref _currentTick);

    /// <summary>
    /// Schedules a task to run after a specific delay in ticks.
    /// </summary>
    public void ScheduleDelayed(long delayInTicks, IWorkItem task)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentOutOfRangeException.ThrowIfNegative(delayInTicks);
        
        long targetTick = CurrentTick + delayInTicks;
        var queue = _scheduledTasks.GetOrAdd(targetTick, static _ => new ConcurrentQueue<IWorkItem>());
        queue.Enqueue(task);
    }

    /// <summary>
    /// Schedules an action to run after a specific delay in ticks.
    /// </summary>
    public void ScheduleDelayed(long delayInTicks, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ScheduleDelayed(delayInTicks, new ActionWorkItem(action));
    }

    /// <summary>
    /// Advances the scheduler by one tick and returns tasks that should run this tick.
    /// </summary>
    public ConcurrentQueue<IWorkItem>? Tick()
    {
        long tick = Interlocked.Increment(ref _currentTick);
        _scheduledTasks.TryRemove(tick, out var tasksToRun);
        return tasksToRun;
    }
}
