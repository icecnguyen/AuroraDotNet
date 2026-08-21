using System;

namespace Aurora.Core.Ids;

/// <summary>
/// Represents a unique identifier for an entity within the game engine.
/// </summary>
public readonly record struct EntityId(int Value) : IEquatable<EntityId>
{
    public static EntityId Invalid => new(-1);
    
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
