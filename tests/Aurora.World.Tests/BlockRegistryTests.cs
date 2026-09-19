using Aurora.World;
using Xunit;

namespace Aurora.World.Tests;

public class BlockRegistryTests
{
    [Fact]
    public void BlockRegistryLoadsAllOfficialBlocksAndStates()
    {
        // Must contain all 1,095 blocks from Minecraft 1.21.4
        Assert.Equal(1095, BlockRegistry.TotalBlockTypes);
        // Must contain all 27,866 states
        Assert.Equal(27866, BlockRegistry.TotalBlockStates);
    }

    [Fact]
    public void BlockRegistryResolvesNamesAndDefaultStatesAccurately()
    {
        // 1. Air and Stone
        Assert.Equal("minecraft:air", BlockRegistry.GetBlockName(0));
        Assert.Equal("minecraft:stone", BlockRegistry.GetBlockName(1));
        Assert.Equal("minecraft:oak_stairs", BlockRegistry.GetBlockName(2940));
        Assert.Equal("minecraft:deepslate", BlockRegistry.GetBlockName(25918));

        // 2. Default state lookups
        Assert.Equal(0, BlockRegistry.GetDefaultStateId("minecraft:air"));
        Assert.Equal(1, BlockRegistry.GetDefaultStateId("minecraft:stone"));
        Assert.Equal(1, BlockRegistry.GetDefaultStateId("stone"));
        Assert.Equal(2940, BlockRegistry.GetDefaultStateId("minecraft:oak_stairs"));

        // 3. New 1.21 blocks (Copper bulb, crafter, heavy core, trial spawner, tuff bricks)
        ushort crafterId = BlockRegistry.GetDefaultStateId("minecraft:crafter");
        Assert.True(crafterId > 0, "Expected crafter block to be registered.");

        ushort heavyCoreId = BlockRegistry.GetDefaultStateId("minecraft:heavy_core");
        Assert.True(heavyCoreId > 0, "Expected heavy_core block to be registered.");
    }

    [Fact]
    public void BlockRegistryMapsItemsToPlacableBlocks()
    {
        // Check placing stone, planks, tnt
        ushort stoneState = BlockRegistry.GetBlockStateFromItem(1); // stone item
        Assert.True(stoneState > 0);
        Assert.Equal("minecraft:stone", BlockRegistry.GetBlockName(stoneState));

        // Check Block.GetName and Block.GetId delegation
        ushort crafterState = Block.GetId("minecraft:crafter");
        Assert.Equal("minecraft:crafter", Block.GetName(crafterState));
        Assert.Equal("minecraft:stone", Block.GetName(1));
    }
}
