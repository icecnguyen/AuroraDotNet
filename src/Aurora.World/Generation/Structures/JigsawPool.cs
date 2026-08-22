#pragma warning disable CA5394 // Random is insecure

using System;
using System.Collections.Generic;

namespace Aurora.World.Generation.Structures;

public sealed class JigsawPool
{
    private readonly IReadOnlyList<(StructureTemplate Template, int Weight)> _elements;
    private readonly int _totalWeight;

    public JigsawPool(IReadOnlyList<(StructureTemplate, int)> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        
        _elements = elements;
        foreach (var element in elements)
        {
            _totalWeight += element.Item2;
        }
    }

    public StructureTemplate? Sample(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        
        if (_totalWeight == 0) return null;
        
        int r = random.Next(_totalWeight);
        int current = 0;
        
        foreach (var element in _elements)
        {
            current += element.Weight;
            if (r < current) return element.Template;
        }
        
        return null;
    }
}
