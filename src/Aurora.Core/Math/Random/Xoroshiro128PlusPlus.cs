using System;
using System.Numerics;

namespace Aurora.Core.Math.Random;

/// <summary>
/// Interface matching Java's RandomSource.
/// Provides methods to generate various random primitives for terrain generation.
/// </summary>
public interface IRandomSource
{
    void SetSeed(long seed);
    int NextInt();
    int NextInt(int bound);
    long NextLong();
    bool NextBoolean();
    float NextFloat();
    double NextDouble();
    double NextGaussian();
}

/// <summary>
/// C# Implementation of Minecraft's Xoroshiro128++ Random Generator.
/// Matches Java's exact bitwise operations to ensure deterministic generation.
/// </summary>
public sealed class Xoroshiro128PlusPlus : IRandomSource
{
    private long _seedLo;
    private long _seedHi;
    
    public Xoroshiro128PlusPlus(long seed)
    {
        SetSeed(seed);
    }

    public Xoroshiro128PlusPlus(long seedLo, long seedHi)
    {
        _seedLo = seedLo;
        _seedHi = seedHi;
        if ((_seedLo | _seedHi) == 0L)
        {
            _seedLo = unchecked((long)11400714819323198485UL);
            _seedHi = 7640891576956012809L;
        }
    }

    public void SetSeed(long seed)
    {
        long lo = seed ^ unchecked((long)0x9E3779B97F4A7C15UL);
        long hi = lo ^ unchecked((long)0x9E3779B97F4A7C15UL);
        _seedLo = MixStafford13(lo);
        _seedHi = MixStafford13(hi);
        if ((_seedLo | _seedHi) == 0L)
        {
            _seedLo = unchecked((long)11400714819323198485UL);
            _seedHi = 7640891576956012809L;
        }
    }

    private static long MixStafford13(long z)
    {
        z = (z ^ (long)((ulong)z >> 30)) * unchecked((long)0xBF58476D1CE4E5B9UL);
        z = (z ^ (long)((ulong)z >> 27)) * unchecked((long)0x94D049BB133111EBUL);
        return z ^ (long)((ulong)z >> 31);
    }

    public long NextLong()
    {
        long s0 = _seedLo;
        long s1 = _seedHi;
        long result = (long)BitOperations.RotateLeft((ulong)(s0 + s1), 17) + s0;
        
        s1 ^= s0;
        _seedLo = (long)BitOperations.RotateLeft((ulong)s0, 49) ^ s1 ^ (s1 << 21);
        _seedHi = (long)BitOperations.RotateLeft((ulong)s1, 28);
        
        return result;
    }

    public int NextInt()
    {
        return (int)NextLong();
    }

    public int NextInt(int bound)
    {
        if (bound <= 0) throw new ArgumentException("Bound must be positive");
        
        long r = (long)((ulong)NextInt() & 0xFFFFFFFFUL);
        long m = r * bound;
        long f = m & 0xFFFFFFFFL;
        
        if (f < bound)
        {
            long threshold = ((long)(~bound + 1)) % bound;
            while (f < threshold)
            {
                r = (long)((ulong)NextInt() & 0xFFFFFFFFUL);
                m = r * bound;
                f = m & 0xFFFFFFFFL;
            }
        }
        return (int)(m >> 32);
    }

    public bool NextBoolean()
    {
        return (NextLong() & 1L) != 0L;
    }

    public float NextFloat()
    {
        return (float)((ulong)NextInt() >> 8) * 5.9604645E-8F;
    }

    public double NextDouble()
    {
        return (double)((ulong)NextLong() >> 11) * 1.1102230246251565E-16;
    }

    private double _nextNextGaussian;
    private bool _haveNextNextGaussian;

    public double NextGaussian()
    {
        if (_haveNextNextGaussian)
        {
            _haveNextNextGaussian = false;
            return _nextNextGaussian;
        }

        double v1, v2, s;
        do
        {
            v1 = 2 * NextDouble() - 1;
            v2 = 2 * NextDouble() - 1;
            s = v1 * v1 + v2 * v2;
        } while (s >= 1 || s == 0);

        double multiplier = System.Math.Sqrt(-2 * System.Math.Log(s) / s);
        _nextNextGaussian = v2 * multiplier;
        _haveNextNextGaussian = true;
        return v1 * multiplier;
    }
}
