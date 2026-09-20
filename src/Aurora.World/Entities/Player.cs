using System;
using Aurora.Core.Ids;

namespace Aurora.World.Entities;

/// <summary>
/// Represents a player entity connected to the server.
/// </summary>
public sealed class Player : Entity
{
    public string Username { get; }
    public Guid Uuid { get; }

    /// <summary>
    /// Player inventory: 46 slots matching Minecraft window 0 layout:
    /// 0: Crafting result, 1-4: Crafting grid, 5-8: Armor, 9-35: Main storage, 36-44: Hotbar, 45: Offhand.
    /// </summary>
    public Inventory Inventory { get; }

    public GameMode GameMode { get; set; } = GameMode.Survival;
    public PlayerAbilities Abilities { get; set; }

    public int SelectedSlot { get; set; } // 0..8

    // Survival mechanics
    public float Health { get; set; } = 20.0f;
    public int FoodLevel { get; set; } = 20;
    public float FoodSaturation { get; set; } = 5.0f;
    public float FoodExhaustion { get; set; }
    public short Air { get; set; } = 300;
    public short Fire { get; set; } = -20;
    public int Score { get; set; }
    public float FallDistance { get; set; }
    public int InvulnerabilityTicks { get; set; }

    public Gameplay.HungerManager HungerManager { get; } = new();
    public Gameplay.BreathManager BreathManager { get; } = new();

    public bool IsSneaking { get; set; }
    public bool IsSprinting { get; set; }

    public bool IsAlive => Health > 0;

    public Player(EntityId id, Guid uuid, string username, GameMode gameMode = GameMode.Survival) : base(id)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);

        Uuid = uuid;
        Username = username;
        GameMode = gameMode;
        Abilities = PlayerAbilities.CreateDefault(gameMode);
        Inventory = new Inventory(46);
    }

    public void SetGameMode(GameMode mode)
    {
        GameMode = mode;
        Abilities = PlayerAbilities.CreateDefault(mode);
    }

    public ItemStack GetHotbarItem(int hotbarSlot)
    {
        if (hotbarSlot < 0 || hotbarSlot >= 9) return ItemStack.Empty;
        return Inventory.GetItem(36 + hotbarSlot);
    }

    public void SetHotbarItem(int hotbarSlot, ItemStack item)
    {
        if (hotbarSlot < 0 || hotbarSlot >= 9) return;
        Inventory.SetItem(36 + hotbarSlot, item);
    }

    public ItemStack GetHeldItem()
    {
        return GetHotbarItem(SelectedSlot);
    }

    public void SetHeldItem(ItemStack item)
    {
        SetHotbarItem(SelectedSlot, item);
    }

    public void Damage(float amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Health = Math.Max(0, Health - amount);
    }

    public void Heal(float amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Health = Math.Min(20.0f, Health + amount);
    }

    public void ResetForRespawn()
    {
        Health = 20.0f;
        Air = 300;
        Fire = -20;
        FallDistance = 0.0f;
        InvulnerabilityTicks = 0;
        HungerManager.Restart(this);
        BreathManager.Reset(this);
    }

    public bool TryPickupItem(ref ItemStack stack, out int changedSlot)
    {
        changedSlot = -1;
        if (stack.IsEmpty) return false;

        bool anyPickedUp = false;

        // 1. Try merging into existing matching stacks in hotbar (36..44) and storage (9..35)
        for (int i = 36; i <= 44; i++)
        {
            var current = Inventory.GetItem(i);
            if (!current.IsEmpty && current.ItemId == stack.ItemId && current.Count < 64)
            {
                int canAdd = Math.Min(64 - current.Count, stack.Count);
                Inventory.SetItem(i, new ItemStack(stack.ItemId, (byte)(current.Count + canAdd)));
                int remaining = stack.Count - canAdd;
                stack = remaining > 0 ? new ItemStack(stack.ItemId, (byte)remaining) : ItemStack.Empty;
                changedSlot = i;
                anyPickedUp = true;
                if (stack.IsEmpty) return true;
            }
        }

        for (int i = 9; i <= 35; i++)
        {
            var current = Inventory.GetItem(i);
            if (!current.IsEmpty && current.ItemId == stack.ItemId && current.Count < 64)
            {
                int canAdd = Math.Min(64 - current.Count, stack.Count);
                Inventory.SetItem(i, new ItemStack(stack.ItemId, (byte)(current.Count + canAdd)));
                int remaining = stack.Count - canAdd;
                stack = remaining > 0 ? new ItemStack(stack.ItemId, (byte)remaining) : ItemStack.Empty;
                changedSlot = i;
                anyPickedUp = true;
                if (stack.IsEmpty) return true;
            }
        }

        // 2. Try placing into first empty slot in hotbar, then storage
        for (int i = 36; i <= 44; i++)
        {
            var current = Inventory.GetItem(i);
            if (current.IsEmpty)
            {
                Inventory.SetItem(i, stack);
                stack = ItemStack.Empty;
                changedSlot = i;
                return true;
            }
        }

        for (int i = 9; i <= 35; i++)
        {
            var current = Inventory.GetItem(i);
            if (current.IsEmpty)
            {
                Inventory.SetItem(i, stack);
                stack = ItemStack.Empty;
                changedSlot = i;
                return true;
            }
        }

        return anyPickedUp;
    }

    public bool TryPickupItem(ref ItemStack stack)
    {
        return TryPickupItem(ref stack, out _);
    }

    public ItemStack DropHeldItem(bool dropEntireStack)
    {
        var held = GetHeldItem();
        if (held.IsEmpty) return ItemStack.Empty;

        if (dropEntireStack || held.Count <= 1)
        {
            SetHeldItem(ItemStack.Empty);
            return held;
        }
        else
        {
            SetHeldItem(new ItemStack(held.ItemId, (byte)(held.Count - 1)));
            return new ItemStack(held.ItemId, 1);
        }
    }
}
