using System;
using System.Collections.Generic;
using System.Numerics;

namespace Aurora.World.AI;

/// <summary>
/// Basic A* Pathfinder stub (Phase 12).
/// </summary>
public sealed class Pathfinder
{
#pragma warning disable CA1822 // Method can be static, but we keep it instance for future state (e.g. caching)
    public IReadOnlyList<Vector3> FindPath(Vector3 start, Vector3 destination, Dimension dimension)
    {
#pragma warning restore CA1822
        ArgumentNullException.ThrowIfNull(dimension);
        
        // MVP: Returns a straight line to the target (Direct approach)
        var path = new List<Vector3>
        {
            start,
            destination
        };
        
        return path;
    }
}
