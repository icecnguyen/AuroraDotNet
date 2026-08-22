using System;
using System.Security.Cryptography;
using System.Text;

namespace Aurora.Core.Math.Random;

/// <summary>
/// Generates deterministic Xoroshiro128++ instances based on coordinates and string salts.
/// Essential for ensuring identical terrain features generate at the same coordinates consistently.
/// </summary>
public sealed class PositionalRandomFactory
{
    private readonly long _seedLo;
    private readonly long _seedHi;

    public PositionalRandomFactory(long seed)
    {
        // Simple initial hash of the global seed
        _seedLo = MixStafford13(seed ^ unchecked((long)0x9E3779B97F4A7C15UL));
        _seedHi = MixStafford13(_seedLo ^ unchecked((long)0x9E3779B97F4A7C15UL));
    }
    
    private PositionalRandomFactory(long seedLo, long seedHi)
    {
        _seedLo = seedLo;
        _seedHi = seedHi;
    }
    
    private static long MixStafford13(long z)
    {
        z = (z ^ (long)((ulong)z >> 30)) * unchecked((long)0xBF58476D1CE4E5B9UL);
        z = (z ^ (long)((ulong)z >> 27)) * unchecked((long)0x94D049BB133111EBUL);
        return z ^ (long)((ulong)z >> 31);
    }

    public IRandomSource At(int x, int y, int z)
    {
        long hash = GetCoordinateHash(x, y, z);
        return new Xoroshiro128PlusPlus(hash ^ _seedLo, hash ^ _seedHi);
    }

    public IRandomSource At(int x, int z)
    {
        long hash = GetCoordinateHash(x, 0, z);
        return new Xoroshiro128PlusPlus(hash ^ _seedLo, hash ^ _seedHi);
    }

    /// <summary>
    /// Creates a new sub-factory salted with a specific string (e.g. "minecraft:aquifer")
    /// </summary>
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
#pragma warning disable CA1850 // Prefer static HashData
    public PositionalRandomFactory FromHashOf(string salt)
    {
        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(salt));
        
        long saltLo = BitConverter.ToInt64(hash, 0);
        long saltHi = BitConverter.ToInt64(hash, 8);
        
        return new PositionalRandomFactory(_seedLo ^ saltLo, _seedHi ^ saltHi);
    }
#pragma warning restore CA1850
#pragma warning restore CA5351

    private static long GetCoordinateHash(int x, int y, int z)
    {
        long hash = (long)(x * 3129871) ^ (long)z * 116129781L ^ (long)y;
        hash = hash * hash * 42317861L + hash * 11L;
        return hash;
    }
}
