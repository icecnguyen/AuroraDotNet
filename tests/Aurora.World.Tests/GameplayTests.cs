using System;
using Aurora.Core.Events;
using Aurora.Core.Ids;
using Aurora.World.Entities;
using Aurora.World.Events;
using Aurora.World.Gameplay;
using Xunit;

namespace Aurora.World.Tests;

public class GameplayTests
{
    [Fact]
    public void BlockBreakEventCanBeCancelled()
    {
        var eventManager = new EventManager();
        var gameplay = new GameplaySystem(eventManager);
        
        var dimension = new Dimension("Test");
        var chunk = FlatWorldGenerator.GenerateChunk(0, 0);
        dimension.SetChunk(chunk);
        
        // Assert bedrock is there
        Assert.Equal(Block.Bedrock, dimension.GetBlockState(0, -64, 0));
        
        // Register a plugin-like cancellation
        eventManager.Register<BlockBreakEvent>(e =>
        {
            if (e.Y == -64) // Cannot break bedrock
            {
                e.IsCancelled = true;
            }
        });
        
        var player = new Player(new EntityId(1), Guid.NewGuid(), "Steve");
        gameplay.BreakBlock(player, dimension, 0, -64, 0);
        
        // Block should still be Bedrock
        Assert.Equal(Block.Bedrock, dimension.GetBlockState(0, -64, 0));
    }
    
    [Fact]
    public void EntityDamageCanBeModified()
    {
        var eventManager = new EventManager();
        var gameplay = new GameplaySystem(eventManager);
        
        var player = new Player(new EntityId(1), Guid.NewGuid(), "Steve");
        Assert.Equal(20.0f, player.Health);
        
        // Register an armor plugin reducing damage
        eventManager.Register<EntityDamageEvent>(e =>
        {
            e.Damage -= 2.0f; // Armor reduces damage by 2
        });
        
        gameplay.DamageEntity(player, 5.0f);
        
        // Took 3.0 damage instead of 5.0
        Assert.Equal(17.0f, player.Health);
    }
}
