using System;
using Aurora.Core.Ids;
using Aurora.Server;
using Aurora.World.Entities;
using Xunit;

namespace Aurora.Server.Tests;

public class PlayerLifecycleTests
{
    [Fact]
    public void PlayerManagerManagesPlayersAccurately()
    {
        var manager = new PlayerManager();
        Assert.Equal(0, manager.PlayerCount);

        var uuid1 = Guid.NewGuid();
        var player1 = new Player(new EntityId(10), uuid1, "Steve", GameMode.Survival);

        var uuid2 = Guid.NewGuid();
        var player2 = new Player(new EntityId(11), uuid2, "Alex", GameMode.Creative);

        manager.AddPlayer(player1);
        manager.AddPlayer(player2);

        Assert.Equal(2, manager.PlayerCount);

        // Lookup by UUID
        Assert.Same(player1, manager.GetPlayer(uuid1));
        Assert.Same(player2, manager.GetPlayer(uuid2));

        // Lookup by username (case-insensitive)
        Assert.Same(player1, manager.GetPlayer("steve"));
        Assert.Same(player2, manager.GetPlayer("ALEX"));
        Assert.Null(manager.GetPlayer("Herobrine"));

        // Remove
        manager.RemovePlayer(uuid1);
        Assert.Equal(1, manager.PlayerCount);
        Assert.Null(manager.GetPlayer(uuid1));
    }

    [Fact]
    public void PlayerLifecycleMechanicsOperateCorrectly()
    {
        var player = new Player(new EntityId(1), Guid.NewGuid(), "Steve", GameMode.Survival);
        Assert.Equal(20.0f, player.Health);
        Assert.True(player.IsAlive);

        // Take damage
        player.Damage(6.5f);
        Assert.Equal(13.5f, player.Health, 0.01f);

        // Heal
        player.Heal(2.5f);
        Assert.Equal(16.0f, player.Health, 0.01f);

        // Overkill
        player.Damage(100.0f);
        Assert.Equal(0.0f, player.Health);
        Assert.False(player.IsAlive);

        // Respawn reset
        player.ResetForRespawn();
        Assert.Equal(20.0f, player.Health);
        Assert.Equal(20, player.FoodLevel);
        Assert.Equal(5.0f, player.FoodSaturation);
        Assert.True(player.IsAlive);

        // Switch to Creative
        player.SetGameMode(GameMode.Creative);
        Assert.Equal(GameMode.Creative, player.GameMode);
        Assert.True(player.Abilities.CreativeMode);
        Assert.True(player.Abilities.Invulnerable);
        Assert.True(player.Abilities.AllowFlying);
    }
}
