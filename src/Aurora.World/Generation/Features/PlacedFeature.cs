#pragma warning disable CA5394 // Random is insecure

using System;
using System.Collections.Generic;
using Aurora.World.Generation.Density;

namespace Aurora.World.Generation.Features;

public sealed class PlacedFeature
{
    public IFeature Feature { get; }
    public int Count { get; }
    public HeightProvider Height { get; }

    public PlacedFeature(IFeature feature, int count, HeightProvider height)
    {
        Feature = feature;
        Count = count;
        Height = height;
    }

    public void Place(Chunk chunk, NoiseContext context, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        
        for (int i = 0; i < Count; i++)
        {
            // Pick a random X, Z in this chunk (0-15)
            int x = random.Next(16);
            int z = random.Next(16);
            
            // Pick a Y according to the HeightProvider distribution
            int y = Height.Sample(random, context);
            
            var featureContext = new FeatureContext(chunk, x, y, z, random);
            Feature.Place(featureContext);
        }
    }
}
