using System.Numerics;
using Aurora.Core.Ids;
using Aurora.World.AI;
using Aurora.World.Simulation;

namespace Aurora.World.Entities;

/// <summary>
/// A hostile mob (Phase 12).
/// </summary>
public sealed class Zombie : Entity
{
    private readonly Pathfinder _pathfinder = new();
    public Player? Target { get; set; }

    public Zombie(EntityId id) : base(id)
    {
    }

    public override void Tick()
    {
        base.Tick();
        
        // Basic AI Goal: Walk towards target
        if (Target != null && CurrentDimension != null)
        {
            var path = _pathfinder.FindPath(Position, Target.Position, CurrentDimension);
            if (path.Count > 1)
            {
                var nextStep = path[1];
                var direction = Vector3.Normalize(nextStep - Position);
                
                // Move towards player
                Velocity = new Vector3(direction.X * 0.1f, Velocity.Y, direction.Z * 0.1f);
            }
        }
        
        PhysicsEngine.Simulate(this);
    }
}
