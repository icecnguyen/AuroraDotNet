using System;
using System.Threading;

namespace Aurora.Threading;

/// <summary>
/// Manages a pool of workers to handle concurrent processing.
/// </summary>
public sealed class WorkerPool : IDisposable
{
    private readonly Worker[] _workers;
    private readonly SchedulerMetrics _metrics;
    private int _roundRobinIndex;

    public SchedulerMetrics Metrics => _metrics;

    public WorkerPool(int workerCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workerCount);

        _metrics = new SchedulerMetrics();
        _workers = new Worker[workerCount];
        for (int i = 0; i < workerCount; i++)
        {
            _workers[i] = new Worker(i, _metrics);
        }
    }

    public void Start()
    {
        foreach (var worker in _workers)
        {
            worker.Start();
        }
    }

    public void EnqueueTask(IWorkItem task)
    {
        ArgumentNullException.ThrowIfNull(task);
        var index = Interlocked.Increment(ref _roundRobinIndex) % _workers.Length;
        _workers[Math.Abs(index)].Enqueue(task);
    }

    public void EnqueueTask(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        EnqueueTask(new ActionWorkItem(action));
    }

    public void Dispose()
    {
        foreach (var worker in _workers)
        {
            worker.Dispose();
        }
    }
}
