using System;
using System.Numerics;
using Aurora.Core.Ids;
using Aurora.World.Entities;
using Aurora.World.Simulation;
using Xunit;

namespace Aurora.World.Tests;

public class SimulationTests
{
    private sealed class TestEntity : Entity
    {
        public TestEntity() : base(new EntityId(1))
        {
        }
    }

    [Fact]
    public void PhysicsEngineAppliesGravityAndCollidesWithFloor()
    {
        var entity = new TestEntity
        {
            Position = new Vector3(0, 10, 0),
            Velocity = new Vector3(0, 0, 0)
        };

        // Tick 1: Gravity applies
        PhysicsEngine.Simulate(entity);
        Assert.True(entity.Velocity.Y < 0);
        Assert.True(entity.Position.Y < 10);
        Assert.False(entity.OnGround);

        // Simulate falling to the floor (Y=0)
        for (int i = 0; i < 100; i++)
        {
            PhysicsEngine.Simulate(entity);
        }

        Assert.Equal(0, entity.Position.Y);
        Assert.Equal(0, entity.Velocity.Y);
        Assert.True(entity.OnGround);
    }
}
