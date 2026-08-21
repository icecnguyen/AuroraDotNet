using System;
using System.Diagnostics;
using Aurora.Core.Metrics;
using Aurora.Core.Pooling;
using Xunit;

namespace Aurora.Core.Tests;

public class OptimizationTests
{
    private sealed class DummyObject
    {
        public int Value { get; set; }
    }

    [Fact]
    public void ObjectPoolReusesObjects()
    {
        var pool = new ObjectPool<DummyObject>(o => o.Value = 0);
        
        var obj1 = pool.Rent();
        obj1.Value = 42;
        pool.Return(obj1);
        
        var obj2 = pool.Rent();
        
        Assert.Same(obj1, obj2); // Should be the exact same instance
        Assert.Equal(0, obj2.Value); // Value should have been reset
    }

    [Fact]
    public void MetricsRegistryTracksTickTime()
    {
        MetricsRegistry.RecordTickTime(50.0);
        MetricsRegistry.RecordTickTime(40.0);
        
        double avg = MetricsRegistry.AverageTickTimeMs;
        
        Assert.Equal(45.0, avg);
        
        long memory = MetricsRegistry.TotalAllocatedMemoryMb;
        Assert.True(memory >= 0); // Can't assert exact memory, just ensure it doesn't crash
    }
}
