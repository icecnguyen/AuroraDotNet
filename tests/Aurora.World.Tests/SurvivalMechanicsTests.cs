using System;
using System.Numerics;
using Aurora.Core.Ids;
using Aurora.World.Entities;
using Xunit;

namespace Aurora.World.Tests;

public class SurvivalMechanicsTests
{
    [Fact]
    public void BlockDropRegistryReturnsCorrectDrops()
    {
        var stoneDrop = BlockDropRegistry.GetDrop(Block.Stone);
        Assert.False(stoneDrop.IsEmpty);
        Assert.Equal("minecraft:cobblestone", ItemRegistry.GetItemName(stoneDrop.ItemId));
        Assert.Equal(1, stoneDrop.Count);

        var grassDrop = BlockDropRegistry.GetDrop(Block.GrassBlock);
        Assert.False(grassDrop.IsEmpty);
        Assert.Equal("minecraft:dirt", ItemRegistry.GetItemName(grassDrop.ItemId));

        var diamondDrop = BlockDropRegistry.GetDrop(Block.DiamondOre);
        Assert.False(diamondDrop.IsEmpty);
        Assert.Equal("minecraft:diamond", ItemRegistry.GetItemName(diamondDrop.ItemId));

        var airDrop = BlockDropRegistry.GetDrop(Block.Air);
        Assert.True(airDrop.IsEmpty);

        var bedrockDrop = BlockDropRegistry.GetDrop(Block.Bedrock);
        Assert.True(bedrockDrop.IsEmpty);
    }

    [Fact]
    public void PlayerPicksUpItemIntoEmptyHotbar()
    {
        var player = new Player(new EntityId(1), Guid.NewGuid(), "Steve", GameMode.Survival);
        int diamondId = ItemRegistry.GetItemId("diamond");

        var incoming = new ItemStack(diamondId, 5);
        bool pickedUp = player.TryPickupItem(ref incoming);

        Assert.True(pickedUp);
        Assert.True(incoming.IsEmpty);
        Assert.Equal(diamondId, player.GetHotbarItem(0).ItemId);
        Assert.Equal(5, player.GetHotbarItem(0).Count);
    }

    [Fact]
    public void PlayerMergesItemStackCorrectly()
    {
        var player = new Player(new EntityId(1), Guid.NewGuid(), "Steve", GameMode.Survival);
        int dirtId = ItemRegistry.GetItemId("dirt");

        // Put 50 dirt in hotbar slot 0
        player.SetHotbarItem(0, new ItemStack(dirtId, 50));

        // Attempt to pickup 20 dirt
        var incoming = new ItemStack(dirtId, 20);
        bool pickedUp = player.TryPickupItem(ref incoming);

        Assert.True(pickedUp);
        // Hotbar slot 0 should now be capped at 64
        Assert.Equal(64, player.GetHotbarItem(0).Count);
        // The remaining 6 dirt should be placed in next empty slot (hotbar slot 1)
        Assert.True(incoming.IsEmpty);
        Assert.Equal(6, player.GetHotbarItem(1).Count);
        Assert.Equal(dirtId, player.GetHotbarItem(1).ItemId);
    }

    [Fact]
    public void PlayerDropsItemSingleAndStack()
    {
        var player = new Player(new EntityId(1), Guid.NewGuid(), "Steve", GameMode.Survival);
        int ironId = ItemRegistry.GetItemId("iron_ingot");
        player.SetHotbarItem(0, new ItemStack(ironId, 5));
        player.SelectedSlot = 0;

        // Drop single (Q)
        var singleDrop = player.DropHeldItem(dropEntireStack: false);
        Assert.Equal(1, singleDrop.Count);
        Assert.Equal(ironId, singleDrop.ItemId);
        Assert.Equal(4, player.GetHeldItem().Count);

        // Drop entire stack (Ctrl+Q)
        var fullDrop = player.DropHeldItem(dropEntireStack: true);
        Assert.Equal(4, fullDrop.Count);
        Assert.Equal(ironId, fullDrop.ItemId);
        Assert.True(player.GetHeldItem().IsEmpty);
    }

    [Fact]
    public void ItemEntityTicksPhysicsAndDespawns()
    {
        var item = new ItemEntity(new Vector3(0, 100, 0), new ItemStack(1, 1), Vector3.Zero, pickupDelay: 5);
        Assert.Equal(5, item.PickupDelay);
        Assert.False(item.IsDead);

        // Tick once with null world (just decrement delay and age)
        item.Tick(null!);
        Assert.Equal(4, item.PickupDelay);
        Assert.Equal(1, item.Age);

        // Fast forward age past 6000
        item.Age = 6000;
        item.Tick(null!);
        Assert.True(item.IsDead);
    }
}
