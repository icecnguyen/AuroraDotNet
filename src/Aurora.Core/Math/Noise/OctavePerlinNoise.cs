using System;
using System.Collections.Generic;
using Aurora.Core.Math.Random;

namespace Aurora.Core.Math.Noise;

/// <summary>
/// Combines multiple octaves of ImprovedNoise matching Java's implementation.
/// </summary>
public sealed class OctavePerlinNoise
{
    private readonly ImprovedNoise[] _noiseLevels;
    private readonly double _lowestFreqValueFactor;
    private readonly double _lowestFreqInputFactor;
    private readonly double _maxValue;

    public OctavePerlinNoise(IRandomSource random, int firstOctave, IReadOnlyList<double> amplitudes)
    {
        System.ArgumentNullException.ThrowIfNull(random);
        System.ArgumentNullException.ThrowIfNull(amplitudes);

        int count = amplitudes.Count;
        _noiseLevels = new ImprovedNoise[count];
        
        // Java consumes randoms sequentially. 
        // We must mimic exact consumption to preserve determinism.
        for (int i = 0; i < count; i++)
        {
            if (amplitudes[i] != 0.0)
            {
                _noiseLevels[i] = new ImprovedNoise(random);
            }
            else
            {
                // Consume random numbers equivalent to one ImprovedNoise creation
                // so the next octave stays perfectly in sync with Java.
                random.NextDouble();
                random.NextDouble();
                random.NextDouble();
                for (int skip = 0; skip < 256; skip++)
                {
                    random.NextInt(256 - skip);
                }
            }
        }

        double highestFreqValueFactor = System.Math.Pow(2.0, firstOctave);
        double highestFreqInputFactor = System.Math.Pow(2.0, count - 1) / (highestFreqValueFactor * System.Math.Pow(2.0, count - 1));
        
        _lowestFreqInputFactor = highestFreqValueFactor;
        _lowestFreqValueFactor = highestFreqInputFactor;

        double expectedMaxValue = 0;
        double factor = 1.0;

        for (int i = 0; i < count; i++)
        {
            if (amplitudes[i] != 0.0)
            {
                expectedMaxValue += factor * amplitudes[i];
            }
            factor /= 2.0;
        }

        _maxValue = expectedMaxValue;
    }

    public double GetValue(double x, double y, double z)
    {
        return GetValue(x, y, z, 0.0, 0.0, false);
    }

    public double GetValue(double x, double y, double z, double yScale, double yMax, bool useOrigin)
    {
        double total = 0.0;
        double inputFactor = _lowestFreqInputFactor;
        double valueFactor = _lowestFreqValueFactor;

        for (int i = 0; i < _noiseLevels.Length; i++)
        {
            var noise = _noiseLevels[i];
            if (noise != null)
            {
                double px = Wrap(x * inputFactor);
                double py = useOrigin ? -noise.yo : Wrap(y * inputFactor);
                double pz = Wrap(z * inputFactor);
                total += noise.Noise(px, py, pz, yScale * inputFactor, yMax * inputFactor) * valueFactor;
            }

            inputFactor *= 2.0;
            valueFactor /= 2.0;
        }

        return total;
    }

    private static double Wrap(double value)
    {
        return value - System.Math.Floor(value / 33554432.0) * 33554432.0;
    }
}
