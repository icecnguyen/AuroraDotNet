namespace Aurora.Core.Math;

/// <summary>
/// Represents a 2D chunk position.
/// </summary>
public readonly record struct ChunkPosition(int X, int Z)
{
    public static ChunkPosition Zero => new(0, 0);

    public long ToRegionIndex()
    {
        // Simple mapping to region index assuming 32x32 chunks per region (512x512 blocks)
        return ((long)(X >> 5) << 32) | ((long)(Z >> 5) & 0xFFFFFFFFL);
    }
    
    public override string ToString() => $"ChunkPosition({X}, {Z})";

    public long LongHash => ((long)X << 32) | ((long)Z & 0xFFFFFFFFL);
}
