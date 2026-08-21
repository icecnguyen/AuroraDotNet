using System;
using Aurora.Core.Events;
using Aurora.World.Entities;
using Aurora.World.Events;

namespace Aurora.World.Gameplay;

/// <summary>
/// Handles core gameplay logic (Block breaking, Item drops, Combat).
/// </summary>
public sealed class GameplaySystem
{
    private readonly EventManager _eventManager;

    public GameplaySystem(EventManager eventManager)
    {
        _eventManager = eventManager;
    }

    public void BreakBlock(Player player, Dimension dimension, int x, int y, int z)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(dimension);
        
        var ev = new BlockBreakEvent(player, dimension, x, y, z);
        _eventManager.Dispatch(ev);

        if (!ev.IsCancelled)
        {
            // Update dimension
            dimension.SetBlockState(x, y, z, Block.Air);
            
            // In a full implementation, we would spawn an ItemEntity here
        }
    }

    public void DamageEntity(Entity target, float amount)
    {
        ArgumentNullException.ThrowIfNull(target);
        
        var ev = new EntityDamageEvent(target, amount);
        _eventManager.Dispatch(ev);

        if (!ev.IsCancelled && target is Player p)
        {
            p.Damage(ev.Damage);
        }
    }
}
