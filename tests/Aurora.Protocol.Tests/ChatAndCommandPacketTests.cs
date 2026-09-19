using System;
using System.Buffers;
using System.Text;
using Aurora.Protocol;
using Aurora.Protocol.Play;
using Xunit;

namespace Aurora.Protocol.Tests;

public class ChatAndCommandPacketTests
{
    private static byte[] WriteToBytes(IPacket packet)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new PacketWriter(buffer);
        packet.Write(ref writer);
        return buffer.WrittenSpan.ToArray();
    }

    [Fact]
    public void SystemChatMessagePacketSerializesAnonymousNbtCorrectly()
    {
        var packet = new SystemChatMessagePacket
        {
            Content = "§aSet game mode to Creative Mode",
            Overlay = false
        };

        Assert.Equal(0x73, packet.PacketId);

        byte[] data = WriteToBytes(packet);
        byte[] expectedUtf8 = Encoding.UTF8.GetBytes("§aSet game mode to Creative Mode");

        // Format:
        // Byte 0: Type ID = 8 (TAG_String)
        // Byte 1-2: Big-endian ushort length
        // Byte 3..N+2: UTF-8 string payload
        // Byte N+3: Overlay bool (0 = false)
        Assert.Equal(1 + 2 + expectedUtf8.Length + 1, data.Length);
        Assert.Equal(8, data[0]); // TAG_String
        
        ushort len = (ushort)((data[1] << 8) | data[2]);
        Assert.Equal(expectedUtf8.Length, len);

        var payload = new ReadOnlySpan<byte>(data, 3, len);
        Assert.True(payload.SequenceEqual(expectedUtf8));

        Assert.Equal(0, data[^1]); // Overlay = false
    }

    [Fact]
    public void SystemChatMessagePacketWithOverlaySerializesCorrectly()
    {
        var packet = new SystemChatMessagePacket
        {
            Content = "Actionbar message",
            Overlay = true
        };

        byte[] data = WriteToBytes(packet);
        Assert.Equal(1, data[^1]); // Overlay = true
    }

    [Fact]
    public void DeclareCommandsPacketSerializesValidBrigadierGraph()
    {
        var packet = new DeclareCommandsPacket();
        Assert.Equal(0x11, packet.PacketId);

        byte[] data = WriteToBytes(packet);
        Assert.NotEmpty(data);

        // Read node count:
        var seq = new ReadOnlySequence<byte>(data);
        var reader = new PacketReader(seq);
        int nodeCount = reader.ReadVarInt();
        Assert.Equal(17, nodeCount);
    }

    [Fact]
    public void UpdateTimePacketSerializesCorrectly()
    {
        var packet = new UpdateTimePacket
        {
            WorldAge = 12345,
            TimeOfDay = 1000,
            IsIncreasing = true
        };

        Assert.Equal(0x6B, packet.PacketId);

        byte[] data = WriteToBytes(packet);
        Assert.Equal(17, data.Length); // 8 bytes long + 8 bytes long + 1 byte bool

        var seq = new ReadOnlySequence<byte>(data);
        var reader = new PacketReader(seq);

        long worldAge = reader.ReadLong();
        long timeOfDay = reader.ReadLong();
        bool isIncreasing = reader.ReadByte() != 0;

        Assert.Equal(12345, worldAge);
        Assert.Equal(1000, timeOfDay);
        Assert.True(isIncreasing);
    }

    [Fact]
    public void CommandSuggestionsPacketsSerializeAndDeserialize()
    {
        // Serverbound 0x0D
        var req = new CommandSuggestionRequestPacket
        {
            TransactionId = 99,
            Text = "/gamemode "
        };
        Assert.Equal(0x0D, req.PacketId);

        byte[] reqData = WriteToBytes(req);
        var reqSeq = new ReadOnlySequence<byte>(reqData);
        var reqReader = new PacketReader(reqSeq);

        var loadedReq = new CommandSuggestionRequestPacket();
        loadedReq.Read(ref reqReader);

        Assert.Equal(99, loadedReq.TransactionId);
        Assert.Equal("/gamemode ", loadedReq.Text);

        // Clientbound 0x10
        var resp = new CommandSuggestionsResponsePacket
        {
            TransactionId = 99,
            Start = 10,
            Length = 0,
            Matches = new[] { "survival", "creative", "adventure", "spectator" }
        };
        Assert.Equal(0x10, resp.PacketId);

        byte[] respData = WriteToBytes(resp);
        Assert.NotEmpty(respData);

        var respSeq = new ReadOnlySequence<byte>(respData);
        var respReader = new PacketReader(respSeq);

        Assert.Equal(99, respReader.ReadVarInt());
        Assert.Equal(10, respReader.ReadVarInt());
        Assert.Equal(0, respReader.ReadVarInt());
        Assert.Equal(4, respReader.ReadVarInt());
        Assert.Equal("survival", respReader.ReadString());
        Assert.Equal(0, respReader.ReadByte()); // tooltip = false
    }

    [Fact]
    public void ChatCommandServerboundPacketsReadCorrectly()
    {
        // ChatCommandServerboundPacket (0x05)
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new PacketWriter(buffer);
        writer.WriteString("heal");

        var seq = new ReadOnlySequence<byte>(buffer.WrittenMemory);
        var reader = new PacketReader(seq);
        var cmd = new ChatCommandServerboundPacket();
        cmd.Read(ref reader);
        Assert.Equal(0x05, cmd.PacketId);
        Assert.Equal("heal", cmd.Command);

        // ChatCommandSignedServerboundPacket (0x06)
        buffer.Clear();
        writer = new PacketWriter(buffer);
        writer.WriteString("gamemode creative");

        seq = new ReadOnlySequence<byte>(buffer.WrittenMemory);
        reader = new PacketReader(seq);
        var signedCmd = new ChatCommandSignedServerboundPacket();
        signedCmd.Read(ref reader);
        Assert.Equal(0x06, signedCmd.PacketId);
        Assert.Equal("gamemode creative", signedCmd.Command);
    }
}
