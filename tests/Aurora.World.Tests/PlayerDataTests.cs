using System;
using System.IO;
using System.Numerics;
using Aurora.Core.Ids;
using Aurora.World;
using Aurora.World.Entities;
using Aurora.World.Storage;
using Xunit;

namespace Aurora.World.Tests;

public class PlayerDataTests
{
    [Fact]
    public void ItemRegistryLoadsAllVanillaItemsAccurately()
    {
        Assert.True(ItemRegistry.ItemCount >= 1385, $"Expected at least 1385 items, got {ItemRegistry.ItemCount}");

        // Air is item 0
        Assert.Equal("minecraft:air", ItemRegistry.GetItemName(0));
        Assert.Equal(0, ItemRegistry.GetItemId("minecraft:air"));
        Assert.Equal(0, ItemRegistry.GetItemId("air"));

        // Stone
        int stoneId = ItemRegistry.GetItemId("minecraft:stone");
        Assert.True(stoneId > 0, "Stone item ID should be greater than 0");
        Assert.Equal("minecraft:stone", ItemRegistry.GetItemName(stoneId));
        Assert.Equal(stoneId, ItemRegistry.GetItemId("stone"));

        // Diamond
        int diamondId = ItemRegistry.GetItemId("minecraft:diamond");
        Assert.True(diamondId > 0, "Diamond item ID should be greater than 0");
        Assert.Equal("minecraft:diamond", ItemRegistry.GetItemName(diamondId));
    }

    [Fact]
    public void PlayerDataStorageSavesAndLoadsAccurately()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "aurora_player_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var uuid = Guid.NewGuid();
            var entityId = new EntityId(42);
            var originalPlayer = new Player(entityId, uuid, "Alex", GameMode.Survival)
            {
                Position = new Vector3(123.5f, 64.0f, -456.75f),
                Yaw = 90.0f,
                Pitch = -15.5f,
                Velocity = new Vector3(0.1f, -0.0784f, 0.0f),
                OnGround = true,
                Health = 18.5f,
                FoodLevel = 19,
                FoodSaturation = 4.5f,
                FoodExhaustion = 1.2f,
                Air = 280,
                Fire = -20,
                Score = 1500,
                SelectedSlot = 3
            };

            // Set items in hotbar and armor
            int stoneId = ItemRegistry.GetItemId("minecraft:stone");
            int diamondId = ItemRegistry.GetItemId("minecraft:diamond");
            originalPlayer.SetHotbarItem(0, new ItemStack(stoneId, 64));
            originalPlayer.SetHotbarItem(3, new ItemStack(diamondId, 12));

            // Save
            PlayerDataStorage.Save(originalPlayer, tempDir);

            string savedPath = Path.Combine(tempDir, "playerdata", $"{uuid}.dat");
            Assert.True(File.Exists(savedPath), "Player data file must exist on disk");

            // Load back
            var loadedPlayer = PlayerDataStorage.Load(uuid, tempDir, entityId, "Alex");
            Assert.NotNull(loadedPlayer);
            Assert.Equal(uuid, loadedPlayer.Uuid);
            Assert.Equal("Alex", loadedPlayer.Username);
            Assert.Equal(GameMode.Survival, loadedPlayer.GameMode);

            // Coordinates
            Assert.Equal(123.5f, loadedPlayer.Position.X, 0.01f);
            Assert.Equal(64.0f, loadedPlayer.Position.Y, 0.01f);
            Assert.Equal(-456.75f, loadedPlayer.Position.Z, 0.01f);
            Assert.Equal(90.0f, loadedPlayer.Yaw, 0.01f);
            Assert.Equal(-15.5f, loadedPlayer.Pitch, 0.01f);
            Assert.True(loadedPlayer.OnGround);

            // Health & Food
            Assert.Equal(18.5f, loadedPlayer.Health, 0.01f);
            Assert.Equal(19, loadedPlayer.FoodLevel);
            Assert.Equal(4.5f, loadedPlayer.FoodSaturation, 0.01f);
            Assert.Equal(3, loadedPlayer.SelectedSlot);
            Assert.Equal(1500, loadedPlayer.Score);

            // Inventory
            var item0 = loadedPlayer.GetHotbarItem(0);
            Assert.Equal(stoneId, item0.ItemId);
            Assert.Equal(64, item0.Count);

            var item3 = loadedPlayer.GetHotbarItem(3);
            Assert.Equal(diamondId, item3.ItemId);
            Assert.Equal(12, item3.Count);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void PlayerDataStorageLoadsOfficialVanillaPlayerData()
    {
        string vanillaDatDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tools", "VanillaServer", "world");
        vanillaDatDir = Path.GetFullPath(vanillaDatDir);

        if (!Directory.Exists(Path.Combine(vanillaDatDir, "playerdata")))
        {
            return; // Skip if run outside repository context
        }

        var sampleUuid = Guid.Parse("acc7643e-058f-3e5a-a7b0-df3eb81d4954");
        var loadedPlayer = PlayerDataStorage.Load(sampleUuid, vanillaDatDir, new EntityId(1), "VanillaPlayer");

        Assert.NotNull(loadedPlayer);
        Assert.Equal(sampleUuid, loadedPlayer.Uuid);
        Assert.Equal("VanillaPlayer", loadedPlayer.Username);
        Assert.True(loadedPlayer.Health > 0, "Vanilla player should have health > 0");
        Assert.True(loadedPlayer.FoodLevel > 0, "Vanilla player should have food > 0");
        Assert.True(loadedPlayer.Position.Y > -64, "Vanilla player Y should be within world bounds");
    }
}
