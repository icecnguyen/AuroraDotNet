using System.Threading;
using Xunit;

namespace Aurora.Threading.Tests;

public class GameSchedulerTests
{
    [Fact]
    public void ScheduleDelayedExecutesAtCorrectTick()
    {
        var scheduler = new GameScheduler();
        
        bool executed1 = false;
        bool executed2 = false;

        scheduler.ScheduleDelayed(1, () => executed1 = true);
        scheduler.ScheduleDelayed(3, () => executed2 = true);

        // Tick 1
        var tasks = scheduler.Tick();
        Assert.NotNull(tasks);
        Assert.Single(tasks);
        
        while (tasks.TryDequeue(out var task))
            task.Execute();
            
        Assert.True(executed1);
        Assert.False(executed2);

        // Tick 2
        tasks = scheduler.Tick();
        Assert.Null(tasks);

        // Tick 3
        tasks = scheduler.Tick();
        Assert.NotNull(tasks);
        Assert.Single(tasks);
        
        while (tasks.TryDequeue(out var task))
            task.Execute();
            
        Assert.True(executed2);
    }
}
