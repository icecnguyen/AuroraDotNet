using System;

namespace Aurora.World.Entities;

/// <summary>
/// Manages a collection of items for an entity (like a Player's backpack).
/// </summary>
public sealed class Inventory
{
    private readonly ItemStack[] _slots;

    public int Capacity => _slots.Length;

    public Inventory(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _slots = new ItemStack[capacity];
        for (int i = 0; i < capacity; i++)
        {
            _slots[i] = ItemStack.Empty;
        }
    }

    public ItemStack GetItem(int slot)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, _slots.Length);
        
        return _slots[slot];
    }

    public void SetItem(int slot, ItemStack item)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, _slots.Length);

        _slots[slot] = item;
    }

    public void Clear()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i] = ItemStack.Empty;
        }
    }
}
