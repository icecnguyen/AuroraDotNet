using Aurora.Core.Ids;
using Aurora.World.Entities;

namespace Aurora.World.Events;

public sealed class BlockBreakEvent : Aurora.Core.Events.ICancellableEvent
{
    public Player Player { get; }
    public Dimension Dimension { get; }
    public int X { get; }
    public int Y { get; }
    public int Z { get; }
    
    public bool IsCancelled { get; set; }

    public BlockBreakEvent(Player player, Dimension dimension, int x, int y, int z)
    {
        Player = player;
        Dimension = dimension;
        X = x;
        Y = y;
        Z = z;
    }
}
