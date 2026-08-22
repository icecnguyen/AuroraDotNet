using System;

namespace Aurora.World.Generation.Density;

/// <summary>
/// Provides the X, Y, Z context for Density Functions.
/// </summary>
public readonly struct NoiseContext : IEquatable<NoiseContext>
{
    public int BlockX { get; }
    public int BlockY { get; }
    public int BlockZ { get; }
    
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public NoiseContext(int blockX, int blockY, int blockZ)
    {
        BlockX = blockX;
        BlockY = blockY;
        BlockZ = blockZ;
        X = blockX;
        Y = blockY;
        Z = blockZ;
    }
    
    public NoiseContext(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
        BlockX = (int)Math.Floor(x);
        BlockY = (int)Math.Floor(y);
        BlockZ = (int)Math.Floor(z);
    }

    public bool Equals(NoiseContext other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    }

    public override bool Equals(object? obj)
    {
        return obj is NoiseContext other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    public static bool operator ==(NoiseContext left, NoiseContext right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NoiseContext left, NoiseContext right)
    {
        return !left.Equals(right);
    }
}
