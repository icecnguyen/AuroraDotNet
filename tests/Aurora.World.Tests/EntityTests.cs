using System;
using Aurora.Core.Ids;
using Aurora.World.Entities;
using Xunit;

namespace Aurora.World.Tests;

public class EntityTests
{
    [Fact]
    public void PlayerHealthMechanicsWorkCorrectly()
    {
        var player = new Player(new EntityId(1), Guid.NewGuid(), "Steve");
        
        Assert.Equal(20.0f, player.Health);
        
        player.Damage(5.5f);
        Assert.Equal(14.5f, player.Health);
        
        player.Heal(2.0f);
        Assert.Equal(16.5f, player.Health);
        
        // Cannot exceed max
        player.Heal(100.0f);
        Assert.Equal(20.0f, player.Health);
        
        // Cannot drop below 0
        player.Damage(100.0f);
        Assert.Equal(0.0f, player.Health);
    }
    
    [Fact]
    public void InventoryStoresAndRetrievesItems()
    {
        var player = new Player(new EntityId(1), Guid.NewGuid(), "Steve");
        var stack = new ItemStack(1, 64);
        
        player.Inventory.SetItem(0, stack);
        
        var retrieved = player.Inventory.GetItem(0);
        Assert.Equal(1, retrieved.ItemId);
        Assert.Equal(64, retrieved.Count);
        Assert.False(retrieved.IsEmpty);
        
        // Other slots should be empty
        var emptySlot = player.Inventory.GetItem(1);
        Assert.True(emptySlot.IsEmpty);
    }
}
