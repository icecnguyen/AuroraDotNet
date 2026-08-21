using System;
using System.Collections.Concurrent;

namespace Aurora.Server;

/// <summary>
/// Manages dimensions and active worlds.
/// </summary>
public sealed class WorldManager
{
    private readonly ConcurrentDictionary<string, object> _worlds = new();

    public void AddWorld(string name, object world)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(world);
        _worlds.TryAdd(name, world);
    }

    public object? GetWorld(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        _worlds.TryGetValue(name, out var world);
        return world;
    }
}
