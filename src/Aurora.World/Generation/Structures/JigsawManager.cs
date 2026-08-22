using System;
using System.Collections.Generic;

namespace Aurora.World.Generation.Structures;

public sealed class PlacedStructure
{
    public StructureTemplate Template { get; }
    public int X { get; }
    public int Y { get; }
    public int Z { get; }
    // In a real implementation we also need Rotation and Mirrored properties to correctly orient the structure.

    public PlacedStructure(StructureTemplate template, int x, int y, int z)
    {
        Template = template;
        X = x;
        Y = y;
        Z = z;
    }

    public bool Intersects(PlacedStructure other)
    {
        ArgumentNullException.ThrowIfNull(other);
        
        return !(X >= other.X + other.Template.SizeX ||
                 X + Template.SizeX <= other.X ||
                 Y >= other.Y + other.Template.SizeY ||
                 Y + Template.SizeY <= other.Y ||
                 Z >= other.Z + other.Template.SizeZ ||
                 Z + Template.SizeZ <= other.Z);
    }
}

public sealed class JigsawManager
{
    private readonly Func<string, JigsawPool?> _poolResolver;

    public JigsawManager(Func<string, JigsawPool?> poolResolver)
    {
        _poolResolver = poolResolver;
    }

    /// <summary>
    /// Assembles a multi-part structure using the Jigsaw algorithm (Vanilla 1.14+).
    /// Uses Breadth-First Search to recursively attach pieces up to a max depth.
    /// </summary>
    public IReadOnlyList<PlacedStructure> Assemble(StructureTemplate startPool, int startX, int startY, int startZ, int maxDepth, Random random)
    {
        var placedStructures = new List<PlacedStructure>();
        var queue = new Queue<(PlacedStructure Structure, int Depth)>();
        
        var root = new PlacedStructure(startPool, startX, startY, startZ);
        placedStructures.Add(root);
        queue.Enqueue((root, 0));
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            
            if (current.Depth >= maxDepth) continue;
            
            foreach (var block in current.Structure.Template.JigsawBlocks)
            {
                // Resolve the pool this jigsaw block wants to draw from
                var targetPool = _poolResolver(block.Pool);
                if (targetPool == null) continue;
                
                // Draw a random piece from the pool
                var nextTemplate = targetPool.Sample(random);
                if (nextTemplate == null) continue;
                
                // Find a matching jigsaw block in the next template
                JigsawBlock? matchingBlock = null;
                foreach (var candidate in nextTemplate.JigsawBlocks)
                {
                    // A connection is valid if the Target of the source equals the Name of the candidate
                    // AND the Target of the candidate equals the Name of the source.
                    if (candidate.Name == block.Target && candidate.Target == block.Name)
                    {
                        matchingBlock = candidate;
                        break;
                    }
                }
                
                if (matchingBlock == null) continue;
                
                // Calculate the offset to align the two jigsaw blocks.
                // Normally we apply 3D rotation matrices here based on block facing directions.
                // For this implementation, we assume basic translation.
                int offsetX = (current.Structure.X + block.X) - matchingBlock.Value.X;
                int offsetY = (current.Structure.Y + block.Y) - matchingBlock.Value.Y;
                int offsetZ = (current.Structure.Z + block.Z) - matchingBlock.Value.Z;
                
                var newPlaced = new PlacedStructure(nextTemplate, offsetX, offsetY, offsetZ);
                
                // Check intersection with all existing pieces
                bool overlaps = false;
                foreach (var existing in placedStructures)
                {
                    if (newPlaced.Intersects(existing))
                    {
                        overlaps = true;
                        break;
                    }
                }
                
                if (!overlaps)
                {
                    placedStructures.Add(newPlaced);
                    queue.Enqueue((newPlaced, current.Depth + 1));
                }
            }
        }
        
        return placedStructures;
    }
}
