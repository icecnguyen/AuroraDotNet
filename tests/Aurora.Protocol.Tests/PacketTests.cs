using System.Buffers;
using Aurora.Protocol.Handshake;
using Xunit;

namespace Aurora.Protocol.Tests;

public class PacketTests
{
    [Fact]
    public void HandshakePacketReadAndWriteMatches()
    {
        var packet = new HandshakePacket
        {
            ProtocolVersion = 763, // 1.20.1
            ServerAddress = "localhost",
            ServerPort = 25565,
            NextState = ConnectionState.Status
        };

        var writer = new ArrayBufferWriter<byte>();
        var pWriter = new PacketWriter(writer);
        
        packet.Write(ref pWriter);

        var data = writer.WrittenMemory;
        var seq = new ReadOnlySequence<byte>(data);
        var pReader = new PacketReader(seq);
        
        var decoded = new HandshakePacket();
        decoded.Read(ref pReader);

        Assert.Equal(packet.ProtocolVersion, decoded.ProtocolVersion);
        Assert.Equal(packet.ServerAddress, decoded.ServerAddress);
        Assert.Equal(packet.ServerPort, decoded.ServerPort);
        Assert.Equal(packet.NextState, decoded.NextState);
    }
}
