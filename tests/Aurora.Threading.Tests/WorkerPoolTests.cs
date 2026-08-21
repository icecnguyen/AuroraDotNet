using System;
using System.Threading;
using Xunit;

namespace Aurora.Threading.Tests;

public class WorkerPoolTests
{
    [Fact]
    public void WorkerPoolStartsAndExecutesTasks()
    {
        using var pool = new WorkerPool(2);
        pool.Start();

        using var mre = new ManualResetEventSlim(false);
        int counter = 0;

        for (int i = 0; i < 10; i++)
        {
            pool.EnqueueTask(() =>
            {
                if (Interlocked.Increment(ref counter) == 10)
                {
                    mre.Set();
                }
            });
        }

        Assert.True(mre.Wait(TimeSpan.FromSeconds(5)), "Tasks did not complete in time.");
        
        // Wait a tiny bit for metrics to be recorded by the finally block
        Thread.Sleep(50);
        
        Assert.Equal(10, pool.Metrics.CompletedTasks);
        Assert.Equal(0, pool.Metrics.QueuedTasks);
    }
}
