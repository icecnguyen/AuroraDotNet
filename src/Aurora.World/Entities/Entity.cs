using Aurora.Core.Ids;
using System.Numerics;

namespace Aurora.World.Entities;

/// <summary>
/// The base class for all physical objects in the world.
/// </summary>
public abstract class Entity
{
    public EntityId Id { get; }
    
    // Using Vector3 for float precision movement
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public Dimension? CurrentDimension { get; set; }

    public bool OnGround { get; set; }

    protected Entity(EntityId id)
    {
        Id = id;
    }

    public virtual void Tick()
    {
        // Default entity tick
    }
}
