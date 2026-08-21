namespace Aurora.Core.Math;

/// <summary>
/// Represents an immutable 3D coordinate in the Minecraft world.
/// </summary>
public readonly record struct Coordinate(int X, int Y, int Z)
{
    public static Coordinate Zero => new(0, 0, 0);

    public Coordinate Add(int x, int y, int z) => new(X + x, Y + y, Z + z);
    
    public static Coordinate operator +(Coordinate a, Coordinate b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Coordinate operator -(Coordinate a, Coordinate b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public Coordinate Subtract(Coordinate other) => this - other;

    /// <summary>
    /// Gets the chunk position this block coordinate belongs to.
    /// </summary>
    public ChunkPosition ToChunkPosition() => new(X >> 4, Z >> 4);
    
    public override string ToString() => $"({X}, {Y}, {Z})";
}
