using System;
using System.Numerics;

namespace Aurora.World.Entities;

/// <summary>
/// Represents a dropped item entity in the world (EntityType 68 in Minecraft 1.21.4 Protocol 768).
/// </summary>
public sealed class ItemEntity
{
    private static int _entityIdCounter = 500000;

    public int EntityId { get; }
    public Guid Uuid { get; } = Guid.NewGuid();
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public ItemStack Item { get; set; }
    public int PickupDelay { get; set; }
    public int Age { get; set; }
    public bool IsDead { get; set; }

    public ItemEntity(Vector3 position, ItemStack item, Vector3 velocity, int pickupDelay = 10)
    {
        EntityId = System.Threading.Interlocked.Increment(ref _entityIdCounter);
        Position = position;
        Item = item;
        Velocity = velocity;
        PickupDelay = pickupDelay;
    }

    public void Tick(WorldManager world)
    {
        if (IsDead) return;

        if (PickupDelay > 0)
        {
            PickupDelay--;
        }

        Age++;
        if (Age >= 6000) // 5 minutes despawn
        {
            IsDead = true;
            return;
        }

        if (world != null)
        {
            int blockX = (int)Math.Floor(Position.X);
            int blockY = (int)Math.Floor(Position.Y - 0.1f);
            int blockZ = (int)Math.Floor(Position.Z);

            ushort blockBelow = world.GetBlock(blockX, blockY, blockZ);
            bool onGround = blockBelow != Block.Air && blockBelow != Block.Water && blockBelow != Block.Lava;

            if (onGround)
            {
                Velocity = new Vector3(Velocity.X * 0.5f, 0, Velocity.Z * 0.5f);
            }
            else
            {
                // Gravity & air resistance
                float newVy = Math.Max(Velocity.Y - 0.04f, -0.98f);
                Velocity = new Vector3(Velocity.X * 0.98f, newVy, Velocity.Z * 0.98f);
                Position += Velocity;
            }
        }
    }
}
