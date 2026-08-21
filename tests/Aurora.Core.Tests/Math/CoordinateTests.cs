using Xunit;
using Aurora.Core.Math;

namespace Aurora.Core.Tests.Math;

public class CoordinateTests
{
    [Fact]
    public void CoordinateToChunkPositionCalculatesCorrectly()
    {
        // Positive coordinates
        var coord1 = new Coordinate(16, 0, 32);
        Assert.Equal(new ChunkPosition(1, 2), coord1.ToChunkPosition());

        // Negative coordinates
        var coord2 = new Coordinate(-16, 0, -32);
        Assert.Equal(new ChunkPosition(-1, -2), coord2.ToChunkPosition());

        // Fractional chunks
        var coord3 = new Coordinate(15, 0, 15);
        Assert.Equal(new ChunkPosition(0, 0), coord3.ToChunkPosition());

        var coord4 = new Coordinate(-1, 0, -1);
        Assert.Equal(new ChunkPosition(-1, -1), coord4.ToChunkPosition());
    }
}
