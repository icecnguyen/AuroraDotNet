using System;
using System.Runtime.CompilerServices;
using Aurora.Core.Math.Random;

namespace Aurora.Core.Math.Noise;

/// <summary>
/// Java-matching ImprovedNoise (Perlin Noise) implementation.
/// Uses IRandomSource to perfectly match Java's permutation array.
/// </summary>
public sealed class ImprovedNoise
{
    public double xo { get; }
    public double yo { get; }
    public double zo { get; }
    private readonly byte[] _p = new byte[512];

    public ImprovedNoise(IRandomSource random)
    {
        System.ArgumentNullException.ThrowIfNull(random);
        
        xo = random.NextDouble() * 256.0;
        yo = random.NextDouble() * 256.0;
        zo = random.NextDouble() * 256.0;
        
        byte[] permutation = new byte[256];
        for (int i = 0; i < 256; i++)
            permutation[i] = (byte)i;

        for (int i = 0; i < 256; i++)
        {
            int j = random.NextInt(256 - i);
            byte temp = permutation[i];
            permutation[i] = permutation[i + j];
            permutation[i + j] = temp;
        }

        for (int i = 0; i < 256; i++)
        {
            _p[i] = permutation[i];
            _p[i + 256] = permutation[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Fade(double t) => t * t * t * (t * (t * 6.0 - 15.0) + 10.0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Lerp(double t, double a, double b) => a + t * (b - a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Grad(int hash, double x, double y, double z)
    {
        int h = hash & 15;
        double u = h < 8 ? x : y;
        double v = h < 4 ? y : h == 12 || h == 14 ? x : z;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    public double Noise(double x, double y, double z, double yScale, double yMax)
    {
        double x1 = x + xo;
        double y1 = y + yo;
        double z1 = z + zo;
        
        int X = (int)System.Math.Floor(x1) & 255;
        int Y = (int)System.Math.Floor(y1) & 255;
        int Z = (int)System.Math.Floor(z1) & 255;

        x1 -= System.Math.Floor(x1);
        y1 -= System.Math.Floor(y1);
        z1 -= System.Math.Floor(z1);

        double u = Fade(x1);
        double v = Fade(y1);
        double w = Fade(z1);

        int A = _p[X] + Y;
        int AA = _p[A] + Z;
        int AB = _p[A + 1] + Z;
        
        int B = _p[X + 1] + Y;
        int BA = _p[B] + Z;
        int BB = _p[B + 1] + Z;

        double yClamp = 0;
        if (yMax != 0.0)
        {
            double d1 = yMax >= 0 ? yMax - y1 : yMax + y1;
            yClamp = (d1 < 0.0 || d1 > 1.0) ? 0.0 : d1;
        }

        return Lerp(w, Lerp(v, Lerp(u, Grad(_p[AA], x1, y1, z1),
                                     Grad(_p[BA], x1 - 1, y1, z1)),
                             Lerp(u, Grad(_p[AB], x1, y1 - 1, z1),
                                     Grad(_p[BB], x1 - 1, y1 - 1, z1))),
                     Lerp(v, Lerp(u, Grad(_p[AA + 1], x1, y1, z1 - 1),
                                     Grad(_p[BA + 1], x1 - 1, y1, z1 - 1)),
                             Lerp(u, Grad(_p[AB + 1], x1, y1 - 1, z1 - 1),
                                     Grad(_p[BB + 1], x1 - 1, y1 - 1, z1 - 1))));
    }
}
