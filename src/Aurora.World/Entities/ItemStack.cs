using System;

namespace Aurora.World.Entities;

/// <summary>
/// Represents a stack of items.
/// </summary>
#pragma warning disable CA1711 // ItemStack is standard Minecraft terminology
public struct ItemStack : IEquatable<ItemStack>
#pragma warning restore CA1711
{
    public static ItemStack Empty => new(0, 0);

    public int ItemId { get; }
    public byte Count { get; }

    public bool IsEmpty => ItemId == 0 || Count == 0;

    public ItemStack(int itemId, byte count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(itemId);
        ItemId = itemId;
        Count = count;
    }

    public ItemStack WithCount(byte count) => new(ItemId, count);

    public bool Equals(ItemStack other) => ItemId == other.ItemId && Count == other.Count;
    public override bool Equals(object? obj) => obj is ItemStack other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(ItemId, Count);
    public static bool operator ==(ItemStack left, ItemStack right) => left.Equals(right);
    public static bool operator !=(ItemStack left, ItemStack right) => !(left == right);
}
