using System;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using Aurora.Core.Ids;
using Aurora.Core.Nbt;
using Aurora.World.Entities;

namespace Aurora.World.Storage;

/// <summary>
/// Handles persistence of player data to and from Minecraft 1.21.4 GZip-compressed NBT files (.dat).
/// </summary>
public static class PlayerDataStorage
{
    private const int CurrentDataVersion = 4082; // 1.21.4

    /// <summary>
    /// Saves player data to world/playerdata/{UUID}.dat.
    /// </summary>
    public static void Save(Player player, string worldPath)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentException.ThrowIfNullOrEmpty(worldPath);

        string playerdataDir = Path.Combine(worldPath, "playerdata");
        Directory.CreateDirectory(playerdataDir);

        string filePath = Path.Combine(playerdataDir, $"{player.Uuid}.dat");
        string tempPath = filePath + ".tmp";

        var root = new NbtCompound();
        root.Add("DataVersion", new NbtInt(CurrentDataVersion));
        root.Add("playerGameType", new NbtInt((int)player.GameMode));

        // Position: List of 3 doubles
        var posList = new NbtList(NbtTagType.Double);
        posList.Add(new NbtDouble(player.Position.X));
        posList.Add(new NbtDouble(player.Position.Y));
        posList.Add(new NbtDouble(player.Position.Z));
        root.Add("Pos", posList);

        // Rotation: List of 2 floats (Yaw, Pitch)
        var rotList = new NbtList(NbtTagType.Float);
        rotList.Add(new NbtFloat(player.Yaw));
        rotList.Add(new NbtFloat(player.Pitch));
        root.Add("Rotation", rotList);

        // Motion: List of 3 doubles
        var motionList = new NbtList(NbtTagType.Double);
        motionList.Add(new NbtDouble(player.Velocity.X));
        motionList.Add(new NbtDouble(player.Velocity.Y));
        motionList.Add(new NbtDouble(player.Velocity.Z));
        root.Add("Motion", motionList);

        root.Add("Dimension", new NbtString("minecraft:overworld"));
        root.Add("OnGround", new NbtByte((byte)(player.OnGround ? 1 : 0)));
        root.Add("Health", new NbtFloat(player.Health));
        root.Add("foodLevel", new NbtInt(player.FoodLevel));
        root.Add("foodSaturationLevel", new NbtFloat(player.FoodSaturation));
        root.Add("foodExhaustionLevel", new NbtFloat(player.FoodExhaustion));
        root.Add("Air", new NbtShort(player.Air));
        root.Add("Fire", new NbtShort(player.Fire));
        root.Add("FallDistance", new NbtFloat(0.0f));
        root.Add("Score", new NbtInt(player.Score));
        root.Add("SelectedItemSlot", new NbtInt(player.SelectedSlot));

        // Abilities
        var abilities = new NbtCompound();
        abilities.Add("invulnerable", new NbtByte((byte)(player.Abilities.Invulnerable ? 1 : 0)));
        abilities.Add("flying", new NbtByte((byte)(player.Abilities.Flying ? 1 : 0)));
        abilities.Add("mayfly", new NbtByte((byte)(player.Abilities.AllowFlying ? 1 : 0)));
        abilities.Add("instabuild", new NbtByte((byte)(player.Abilities.CreativeMode ? 1 : 0)));
        abilities.Add("mayBuild", new NbtByte(1));
        abilities.Add("flySpeed", new NbtFloat(player.Abilities.FlySpeed));
        abilities.Add("walkSpeed", new NbtFloat(player.Abilities.WalkSpeed));
        root.Add("abilities", abilities);

        // Inventory
        var invList = new NbtList(NbtTagType.Compound);
        for (int i = 0; i < player.Inventory.Capacity; i++)
        {
            var stack = player.Inventory.GetItem(i);
            if (stack.IsEmpty) continue;

            byte nbtSlot = (byte)i;
            if (i >= 36 && i <= 44)
            {
                nbtSlot = (byte)(i - 36); // Hotbar: 0..8
            }
            else if (i >= 5 && i <= 8)
            {
                nbtSlot = (byte)(103 - (i - 5)); // Armor: 100..103
            }
            else if (i == 45)
            {
                nbtSlot = 150; // Offhand (-106)
            }

            var itemCompound = new NbtCompound();
            itemCompound.Add("Slot", new NbtByte(nbtSlot));
            itemCompound.Add("id", new NbtString(ItemRegistry.GetItemName(stack.ItemId)));
            itemCompound.Add("count", new NbtInt(stack.Count));
            invList.Add(itemCompound);
        }
        root.Add("Inventory", invList);

        // Write GZip compressed NBT
        using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var gzip = new GZipStream(fs, CompressionLevel.Optimal))
        {
            NbtWriter.WriteRoot(gzip, root, string.Empty);
        }

        File.Move(tempPath, filePath, overwrite: true);
    }

    /// <summary>
    /// Loads player data from world/playerdata/{UUID}.dat, or returns null if no save file exists.
    /// </summary>
    public static Player? Load(Guid uuid, string worldPath, EntityId entityId, string username)
    {
        ArgumentException.ThrowIfNullOrEmpty(worldPath);
        ArgumentException.ThrowIfNullOrEmpty(username);

        string filePath = Path.Combine(worldPath, "playerdata", $"{uuid}.dat");
        if (!File.Exists(filePath))
        {
            return null;
        }

        NbtCompound root;
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var gzip = new GZipStream(fs, CompressionMode.Decompress))
        {
            root = NbtReader.ReadRoot(gzip);
        }

        var mode = GameMode.Survival;
        if (root.TryGet("playerGameType", out var gmTag) && gmTag is NbtInt gmInt)
        {
            mode = (GameMode)Math.Clamp(gmInt.Value, 0, 3);
        }

        var player = new Player(entityId, uuid, username, mode);

        // Load Position
        if (root.TryGet("Pos", out var posTag) && posTag is NbtList posList && posList.Count >= 3)
        {
            double x = ((NbtDouble)posList[0]).Value;
            double y = ((NbtDouble)posList[1]).Value;
            double z = ((NbtDouble)posList[2]).Value;
            player.Position = new Vector3((float)x, (float)y, (float)z);
        }

        // Load Rotation
        if (root.TryGet("Rotation", out var rotTag) && rotTag is NbtList rotList && rotList.Count >= 2)
        {
            player.Yaw = ((NbtFloat)rotList[0]).Value;
            player.Pitch = ((NbtFloat)rotList[1]).Value;
        }

        // Load Motion
        if (root.TryGet("Motion", out var motionTag) && motionTag is NbtList motionList && motionList.Count >= 3)
        {
            double vx = ((NbtDouble)motionList[0]).Value;
            double vy = ((NbtDouble)motionList[1]).Value;
            double vz = ((NbtDouble)motionList[2]).Value;
            player.Velocity = new Vector3((float)vx, (float)vy, (float)vz);
        }

        // Load OnGround
        if (root.TryGet("OnGround", out var groundTag) && groundTag is NbtByte groundByte)
        {
            player.OnGround = groundByte.Value != 0;
        }

        // Load Health & Hunger
        if (root.TryGet("Health", out var hpTag) && hpTag is NbtFloat hpFloat)
        {
            player.Health = Math.Clamp(hpFloat.Value, 0.0f, 20.0f);
        }

        if (root.TryGet("foodLevel", out var foodTag) && foodTag is NbtInt foodInt)
        {
            player.FoodLevel = Math.Clamp(foodInt.Value, 0, 20);
        }

        if (root.TryGet("foodSaturationLevel", out var satTag) && satTag is NbtFloat satFloat)
        {
            player.FoodSaturation = satFloat.Value;
        }

        if (root.TryGet("foodExhaustionLevel", out var exhTag) && exhTag is NbtFloat exhFloat)
        {
            player.FoodExhaustion = exhFloat.Value;
        }

        if (root.TryGet("Air", out var airTag) && airTag is NbtShort airShort)
        {
            player.Air = airShort.Value;
        }

        if (root.TryGet("Fire", out var fireTag) && fireTag is NbtShort fireShort)
        {
            player.Fire = fireShort.Value;
        }

        if (root.TryGet("Score", out var scoreTag) && scoreTag is NbtInt scoreInt)
        {
            player.Score = scoreInt.Value;
        }

        if (root.TryGet("SelectedItemSlot", out var slotTag) && slotTag is NbtInt slotInt)
        {
            player.SelectedSlot = Math.Clamp(slotInt.Value, 0, 8);
        }

        // Load Abilities
        if (root.TryGet("abilities", out var abTag) && abTag is NbtCompound abCompound)
        {
            if (abCompound.TryGet("invulnerable", out var invTag) && invTag is NbtByte invByte)
                player.Abilities.Invulnerable = invByte.Value != 0;

            if (abCompound.TryGet("flying", out var flyTag) && flyTag is NbtByte flyByte)
                player.Abilities.Flying = flyByte.Value != 0;

            if (abCompound.TryGet("mayfly", out var canFlyTag) && canFlyTag is NbtByte canFlyByte)
                player.Abilities.AllowFlying = canFlyByte.Value != 0;

            if (abCompound.TryGet("instabuild", out var buildTag) && buildTag is NbtByte buildByte)
                player.Abilities.CreativeMode = buildByte.Value != 0;

            if (abCompound.TryGet("flySpeed", out var fsTag) && fsTag is NbtFloat fsFloat)
                player.Abilities.FlySpeed = fsFloat.Value;

            if (abCompound.TryGet("walkSpeed", out var wsTag) && wsTag is NbtFloat wsFloat)
                player.Abilities.WalkSpeed = wsFloat.Value;
        }

        // Load Inventory
        if (root.TryGet("Inventory", out var inventoryTag) && inventoryTag is NbtList invList)
        {
            foreach (var itemTag in invList)
            {
                if (itemTag is not NbtCompound itemComp) continue;

                byte rawSlot = 0;
                if (itemComp.TryGet("Slot", out var sTag) && sTag is NbtByte sByte)
                {
                    rawSlot = sByte.Value;
                }

                string itemIdName = string.Empty;
                if (itemComp.TryGet("id", out var idTag) && idTag is NbtString idStr)
                {
                    itemIdName = idStr.Value;
                }

                int count = 1;
                if (itemComp.TryGet("count", out var cTag))
                {
                    if (cTag is NbtInt cInt) count = cInt.Value;
                    else if (cTag is NbtByte cByte) count = cByte.Value;
                }

                int itemId = ItemRegistry.GetItemId(itemIdName);
                if (itemId <= 0 && !string.Equals(itemIdName, "minecraft:air", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Map rawSlot to window 0 layout:
                // 0..8: Hotbar -> 36..44
                // 9..35: Main inventory -> 9..35
                // 100..103: Armor -> 5..8 (103: Helmet -> 5, 102: Chest -> 6, 101: Leggings -> 7, 100: Boots -> 8)
                // 150 / -106: Offhand -> 45
                int targetSlot = -1;
                if (rawSlot <= 8)
                {
                    targetSlot = 36 + rawSlot;
                }
                else if (rawSlot >= 9 && rawSlot <= 35)
                {
                    targetSlot = rawSlot;
                }
                else if (rawSlot >= 100 && rawSlot <= 103)
                {
                    targetSlot = 5 + (103 - rawSlot);
                }
                else if (rawSlot == 150 || (sbyte)rawSlot == -106)
                {
                    targetSlot = 45;
                }

                if (targetSlot >= 0 && targetSlot < player.Inventory.Capacity)
                {
                    player.Inventory.SetItem(targetSlot, new ItemStack(itemId, (byte)Math.Clamp(count, 1, 64)));
                }
            }
        }

        return player;
    }
}
