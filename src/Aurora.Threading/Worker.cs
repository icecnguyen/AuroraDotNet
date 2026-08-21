using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aurora.Threading;

/// <summary>
/// Represents a worker thread that processes game simulation and scheduling tasks.
/// </summary>
public sealed class Worker : IDisposable
{
    private readonly int _workerId;
    private readonly SchedulerMetrics _metrics;
    private readonly Channel<IWorkItem> _taskQueue;
    private readonly CancellationTokenSource _cts;
    private Task? _runTask;

    public int WorkerId => _workerId;

    public Worker(int workerId, SchedulerMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        _workerId = workerId;
        _metrics = metrics;
        _taskQueue = Channel.CreateUnbounded<IWorkItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _cts = new CancellationTokenSource();
    }

    public void Start()
    {
        _runTask = Task.Factory.StartNew(RunLoopAsync, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
    }

    public void Enqueue(IWorkItem task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _metrics.IncrementQueued();
        _taskQueue.Writer.TryWrite(task);
    }

    private async Task RunLoopAsync()
    {
        try
        {
            await foreach (var task in _taskQueue.Reader.ReadAllAsync(_cts.Token).ConfigureAwait(false))
            {
                try
                {
                    task.Execute();
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    // Ignore or log exceptions in tasks so the worker doesn't crash
                }
                finally
                {
                    _metrics.RecordCompletion();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Worker shutting down
        }
    }

    public void Dispose()
    {
        _taskQueue.Writer.Complete();
        _cts.Cancel();
        try
        {
            _runTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Ignore termination exceptions
        }
        _cts.Dispose();
    }
}
