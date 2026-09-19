using Aurora.World.Generation.Aquifers;
using Aurora.World.Generation.Structures;
using Xunit;

namespace Aurora.World.Tests;

public class AquiferTests
{
    [Fact]
    public void AquiferReturnsLavaAtMantleDepth()
    {
        var aquifer = new AquiferSampler(12345);

        // At Y <= -54, carver must always place lava
        Assert.Equal(Block.Lava, aquifer.GetCarveFluid(0, -55, 0));
        Assert.Equal(Block.Lava, aquifer.GetCarveFluid(100, -60, -100));
    }

    [Fact]
    public void AquiferReturnsAirAboveSeaLevel()
    {
        var aquifer = new AquiferSampler(12345);

        // Above global sea level (62), carvers carve open air
        Assert.Equal(Block.Air, aquifer.GetCarveFluid(0, 70, 0));
        Assert.Equal(Block.Air, aquifer.GetCarveFluid(50, 100, 50));
    }

    [Fact]
    public void NbtTemplateReaderLoadsValidStructureViaNbtEngine()
    {
        var template = NbtTemplateReader.Load("minecraft:village/plains/houses/house_small");

        Assert.NotNull(template);
        Assert.Equal(5, template.SizeX);
        Assert.Equal(5, template.SizeY);
        Assert.Equal(5, template.SizeZ);

        // Check floor cobblestone
        ushort floorBlock = template.GetBlock(0, 0, 0); // (0, 0, 0)
        Assert.True(floorBlock == Block.Cobblestone || floorBlock == Block.OakPlanks);

        // Verify jigsaw block presence
        Assert.NotEmpty(template.JigsawBlocks);
        Assert.Equal("minecraft:building_entrance", template.JigsawBlocks[0].Name);
    }
}
