using System;
using System.Numerics;
using Aurora.Core.Ids;
using Aurora.World.Entities;
using Xunit;

namespace Aurora.World.Tests;

public class AITests
{
    [Fact]
    public void ZombieMovesTowardsTarget()
    {
        var zombie = new Zombie(new EntityId(1))
        {
            Position = new Vector3(0, 10, 0),
            CurrentDimension = new Dimension("Overworld")
        };
        
        var player = new Player(new EntityId(2), Guid.NewGuid(), "Steve")
        {
            Position = new Vector3(10, 10, 10) // Player is diagonally away
        };
        
        zombie.Target = player;
        
        zombie.Tick(); // Zombie should update velocity towards player
        
        // Assert zombie started moving towards positive X and Z
        Assert.True(zombie.Velocity.X > 0);
        Assert.True(zombie.Velocity.Z > 0);
    }
}
