using System;
using System.Numerics;
using Aurora.World.Entities;

namespace Aurora.World.Simulation;

/// <summary>
/// Basic physics engine for entity movement and gravity.
/// </summary>
public static class PhysicsEngine
{
    private const float Gravity = 0.08f;
    private const float TerminalVelocity = -3.92f;
    private const float Drag = 0.98f;

    public static void Simulate(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        // 1. Apply Gravity
        if (!entity.OnGround)
        {
            float newVelY = entity.Velocity.Y - Gravity;
            if (newVelY < TerminalVelocity) newVelY = TerminalVelocity;
            
            entity.Velocity = new Vector3(entity.Velocity.X, newVelY, entity.Velocity.Z);
        }

        // 2. Apply Velocity to Position
        entity.Position += entity.Velocity;

        // 3. Simple floor collision (assuming flat world at Y=0 for MVP)
        if (entity.Position.Y <= 0)
        {
            entity.Position = new Vector3(entity.Position.X, 0, entity.Position.Z);
            entity.Velocity = new Vector3(entity.Velocity.X, 0, entity.Velocity.Z);
            entity.OnGround = true;
        }
        else
        {
            entity.OnGround = false;
        }

        // 4. Apply Drag (Friction / Air resistance)
        entity.Velocity = new Vector3(entity.Velocity.X * Drag, entity.Velocity.Y, entity.Velocity.Z * Drag);
        
        // Zero out tiny velocities
        if (Math.Abs(entity.Velocity.X) < 0.001f) entity.Velocity = new Vector3(0, entity.Velocity.Y, entity.Velocity.Z);
        if (Math.Abs(entity.Velocity.Z) < 0.001f) entity.Velocity = new Vector3(entity.Velocity.X, entity.Velocity.Y, 0);
    }
}
