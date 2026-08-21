using Aurora.World.Entities;

namespace Aurora.World.Events;

public sealed class EntityDamageEvent : Aurora.Core.Events.ICancellableEvent
{
    public Entity Target { get; }
    public float Damage { get; set; }
    
    public bool IsCancelled { get; set; }

    public EntityDamageEvent(Entity target, float damage)
    {
        Target = target;
        Damage = damage;
    }
}
