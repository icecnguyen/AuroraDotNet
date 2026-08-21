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
    
    public Inventory Inventory { get; }
    
    // Survival mechanics
    public float Health { get; set; } = 20.0f;
    public int FoodLevel { get; set; } = 20;

    public Player(EntityId id, Guid uuid, string username) : base(id)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);
        
        Uuid = uuid;
        Username = username;
        // Standard Minecraft player inventory has 36 main slots + 4 armor + 1 offhand
        Inventory = new Inventory(41); 
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
}
