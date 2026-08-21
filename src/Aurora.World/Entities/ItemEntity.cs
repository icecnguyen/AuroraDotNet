using Aurora.Core.Ids;

namespace Aurora.World.Entities;

public sealed class ItemEntity : Entity
{
    public ItemStack Stack { get; set; }
    
    // Time until it can be picked up
    public int PickupDelay { get; set; } = 20;

    public ItemEntity(EntityId id, ItemStack stack) : base(id)
    {
        Stack = stack;
    }

    public override void Tick()
    {
        if (PickupDelay > 0)
        {
            PickupDelay--;
        }
    }
}
