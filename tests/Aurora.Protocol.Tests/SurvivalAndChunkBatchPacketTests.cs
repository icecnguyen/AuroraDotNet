using System;
using System.Buffers;
using Aurora.Protocol;
using Aurora.Protocol.Play;
using Xunit;

namespace Aurora.Protocol.Tests;

public class SurvivalAndChunkBatchPacketTests
{
    private static byte[] WriteToBytes(IPacket packet)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new PacketWriter(buffer);
        packet.Write(ref writer);
        return buffer.WrittenSpan.ToArray();
    }

    [Fact]
    public void ChunkBatchStartPacketHasCorrectId()
    {
        var packet = new ChunkBatchStartPacket();
        Assert.Equal(0x0D, packet.PacketId);
        byte[] bytes = WriteToBytes(packet);
        Assert.Empty(bytes);
    }

    [Fact]
    public void ChunkBatchFinishedPacketRoundTrips()
    {
        var packet = new ChunkBatchFinishedPacket { BatchSize = 25 };
        Assert.Equal(0x0C, packet.PacketId);

        byte[] bytes = WriteToBytes(packet);
        var seq = new ReadOnlySequence<byte>(bytes);
        var reader = new PacketReader(seq);
        var readPacket = new ChunkBatchFinishedPacket();
        readPacket.Read(ref reader);

        Assert.Equal(25, readPacket.BatchSize);
    }

    [Fact]
    public void ChunkBatchReceivedPacketRoundTrips()
    {
        var packet = new ChunkBatchReceivedPacket { DesiredChunksPerTick = 10.5f };
        Assert.Equal(0x08, packet.PacketId);

        byte[] bytes = WriteToBytes(packet);
        var seq = new ReadOnlySequence<byte>(bytes);
        var reader = new PacketReader(seq);
        var readPacket = new ChunkBatchReceivedPacket();
        readPacket.Read(ref reader);

        Assert.Equal(10.5f, readPacket.DesiredChunksPerTick);
    }

    [Fact]
    public void BlockDestroyStagePacketSerializesCorrectly()
    {
        long loc = BlockUpdatePacket.EncodePosition(10, 64, -20);
        var packet = new BlockDestroyStagePacket
        {
            EntityId = 42,
            Location = loc,
            DestroyStage = 3
        };

        Assert.Equal(0x06, packet.PacketId);
        byte[] bytes = WriteToBytes(packet);

        var seq = new ReadOnlySequence<byte>(bytes);
        var reader = new PacketReader(seq);
        int entityId = reader.ReadVarInt();
        long location = reader.ReadLong();
        sbyte stage = (sbyte)reader.ReadByte();

        Assert.Equal(42, entityId);
        Assert.Equal(loc, location);
        Assert.Equal(3, stage);
    }

    [Fact]
    public void TakeItemEntityPacketSerializesCorrectly()
    {
        var packet = new TakeItemEntityPacket
        {
            CollectedEntityId = 100,
            CollectorEntityId = 5,
            PickupCount = 3
        };

        Assert.Equal(0x76, packet.PacketId);
        byte[] bytes = WriteToBytes(packet);

        var seq = new ReadOnlySequence<byte>(bytes);
        var reader = new PacketReader(seq);
        Assert.Equal(100, reader.ReadVarInt());
        Assert.Equal(5, reader.ReadVarInt());
        Assert.Equal(3, reader.ReadVarInt());
    }

    [Fact]
    public void SetItemEntityDataPacketSerializesMetadataCorrectly()
    {
        var packet = new SetItemEntityDataPacket
        {
            EntityId = 500,
            ItemId = 25,
            ItemCount = 4
        };

        Assert.Equal(0x5D, packet.PacketId);
        byte[] bytes = WriteToBytes(packet);

        var seq = new ReadOnlySequence<byte>(bytes);
        var reader = new PacketReader(seq);
        int entityId = reader.ReadVarInt();
        byte index = reader.ReadByte();
        int type = reader.ReadVarInt();
        int count = reader.ReadVarInt();
        int itemId = reader.ReadVarInt();
        int addedComponents = reader.ReadVarInt();
        int removedComponents = reader.ReadVarInt();
        byte terminator = reader.ReadByte();

        Assert.Equal(500, entityId);
        Assert.Equal(8, index); // DATA_ITEM index
        Assert.Equal(7, type);  // ITEM_STACK type
        Assert.Equal(4, count);
        Assert.Equal(25, itemId);
        Assert.Equal(0, addedComponents);
        Assert.Equal(0, removedComponents);
        Assert.Equal(0xFF, terminator);
    }
}
