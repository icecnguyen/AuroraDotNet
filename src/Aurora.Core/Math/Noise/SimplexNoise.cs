using System;
using Aurora.Core.Math.Random;

namespace Aurora.Core.Math.Noise;

/// <summary>
/// Java-matching SimplexNoise implementation.
/// </summary>
public class SimplexNoise
{
    private static readonly int[][] Grad3 = new int[][]
    {
        new int[] {1, 1, 0}, new int[] {-1, 1, 0}, new int[] {1, -1, 0}, new int[] {-1, -1, 0},
        new int[] {1, 0, 1}, new int[] {-1, 0, 1}, new int[] {1, 0, -1}, new int[] {-1, 0, -1},
        new int[] {0, 1, 1}, new int[] {0, -1, 1}, new int[] {0, 1, -1}, new int[] {0, -1, -1},
        new int[] {1, 1, 0}, new int[] {0, -1, 1}, new int[] {-1, 1, 0}, new int[] {0, -1, -1}
    };

    private readonly int[] p = new int[512];
    public double xo { get; }
    public double yo { get; }
    public double zo { get; }

    public SimplexNoise(IRandomSource random)
    {
        System.ArgumentNullException.ThrowIfNull(random);
        
        xo = random.NextDouble() * 256.0;
        yo = random.NextDouble() * 256.0;
        zo = random.NextDouble() * 256.0;
        
        int[] permutation = new int[256];
        for (int i = 0; i < 256; i++)
        {
            permutation[i] = i;
        }

        for (int i = 0; i < 256; i++)
        {
            int j = random.NextInt(256 - i) + i;
            int temp = permutation[i];
            permutation[i] = permutation[j];
            permutation[j] = temp;
            p[i] = permutation[i];
            p[i + 256] = permutation[i];
        }
    }

    private static double Dot(int[] g, double x, double y)
    {
        return g[0] * x + g[1] * y;
    }

    public double GetValue(double x, double y)
    {
        double n0, n1, n2;
        double f2 = 0.5 * (System.Math.Sqrt(3.0) - 1.0);
        double s = (x + y) * f2;
        int i = Floor(x + s);
        int j = Floor(y + s);
        double g2 = (3.0 - System.Math.Sqrt(3.0)) / 6.0;
        double t = (i + j) * g2;
        double X0 = i - t;
        double Y0 = j - t;
        double x0 = x - X0;
        double y0 = y - Y0;

        int i1, j1;
        if (x0 > y0)
        {
            i1 = 1;
            j1 = 0;
        }
        else
        {
            i1 = 0;
            j1 = 1;
        }

        double x1 = x0 - i1 + g2;
        double y1 = y0 - j1 + g2;
        double x2 = x0 - 1.0 + 2.0 * g2;
        double y2 = y0 - 1.0 + 2.0 * g2;

        int ii = i & 255;
        int jj = j & 255;
        int gi0 = p[ii + p[jj]] % 12;
        int gi1 = p[ii + i1 + p[jj + j1]] % 12;
        int gi2 = p[ii + 1 + p[jj + 1]] % 12;

        double t0 = 0.5 - x0 * x0 - y0 * y0;
        if (t0 < 0) n0 = 0.0;
        else
        {
            t0 *= t0;
            n0 = t0 * t0 * Dot(Grad3[gi0], x0, y0);
        }

        double t1 = 0.5 - x1 * x1 - y1 * y1;
        if (t1 < 0) n1 = 0.0;
        else
        {
            t1 *= t1;
            n1 = t1 * t1 * Dot(Grad3[gi1], x1, y1);
        }

        double t2 = 0.5 - x2 * x2 - y2 * y2;
        if (t2 < 0) n2 = 0.0;
        else
        {
            t2 *= t2;
            n2 = t2 * t2 * Dot(Grad3[gi2], x2, y2);
        }

        return 70.0 * (n0 + n1 + n2);
    }

    private static int Floor(double x)
    {
        int xi = (int)x;
        return x < xi ? xi - 1 : xi;
    }
}
