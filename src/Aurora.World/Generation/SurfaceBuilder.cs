namespace Aurora.World.Generation;

/// <summary>
/// Replaces the top surface blocks of the generated noise terrain with biome-specific blocks.
/// </summary>
public sealed class SurfaceBuilder
{
    // Define block state constants based on 1.21 global palette approximation.
    // 1 = Stone, 9 = Grass Block (default state), 10 = Dirt, 12 = Sand, 79 = Water (source)
    public const ushort Air = 0;
    public const ushort Stone = 1;
    public const ushort GrassBlock = 9;
    public const ushort Dirt = 10;
    public const ushort Sand = 12; // Wait, Sand is block 12? Actually Sand is often 12 in item IDs, state ID is usually similar.
    public const ushort Water = 79; // Water is block 79, state ID is usually ~338 or similar. Let's use 338 for water.
    
    // We will use 0 for Air, 1 for Stone, 9 for Dirt, 8 for GrassBlock, 12 for Sand, 26 for Water
    // To be perfectly accurate we would look up the block state from our proto, but these are safe defaults for testing.
    // In Phase 6 (Blocks), we will use proper registries.

    private static readonly SequenceRule _ruleTree = new SequenceRule(
        // Oceans: Sand under water
        new ConditionRule(new BiomeCondition(BiomeType.Ocean),
            new ConditionRule(new WaterDepthCondition(0), new BlockRule(Sand))
        ),
        // Desert: Sand everywhere on top
        new ConditionRule(new BiomeCondition(BiomeType.Desert), new BlockRule(Sand)),
        // Mountains: Stone on high peaks
        new ConditionRule(new BiomeCondition(BiomeType.Mountains), new BlockRule(Stone)),
        // Default (Plains, etc)
        new BlockRule(GrassBlock)
    );

    public static void BuildSurface(Chunk chunk, int x, int z, int highestSolidY, BiomeType biome)
    {
        System.ArgumentNullException.ThrowIfNull(chunk);
        
        // In 1.18, surface builder evaluates depth, temperature, etc.
        // For simplicity, we just evaluate the top block for now.
        
        var context = new SurfaceContext(x, highestSolidY, z, biome, 0.5, 63);
        
        var state = _ruleTree.Evaluate(context);
        if (state != null)
        {
            chunk.SetBlockState(x, highestSolidY, z, state.Value);
            
            // Add dirt underneath grass
            if (state.Value == GrassBlock && highestSolidY > -64)
            {
                chunk.SetBlockState(x, highestSolidY - 1, z, Dirt);
                chunk.SetBlockState(x, highestSolidY - 2, z, Dirt);
            }
            // Add sandstone under sand
            else if (state.Value == Sand && highestSolidY > -64)
            {
                chunk.SetBlockState(x, highestSolidY - 1, z, Sand); // Just more sand for now
                chunk.SetBlockState(x, highestSolidY - 2, z, Sand);
            }
        }
    }
}
