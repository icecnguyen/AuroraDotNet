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

    [Fact]
    public void EntityEventPacketRoundTrips()
    {
        var packet = new EntityEventPacket
        {
            EntityId = 1234,
            EventId = 2 // Hurt
        };
        Assert.Equal(0x1F, packet.PacketId);

        byte[] bytes = WriteToBytes(packet);
        var seq = new ReadOnlySequence<byte>(bytes);
        var reader = new PacketReader(seq);
        var readPacket = new EntityEventPacket();
        readPacket.Read(ref reader);

        Assert.Equal(1234, readPacket.EntityId);
        Assert.Equal(2, readPacket.EventId);
    }

    [Fact]
    public void SetContainerSlotPacketRoundTrips()
    {
        var packet = new SetContainerSlotPacket
        {
            WindowId = 0,
            StateId = 12,
            Slot = 36,
            ItemId = 45,
            ItemCount = 16
        };
        Assert.Equal(0x15, packet.PacketId);

        byte[] bytes = WriteToBytes(packet);
        var seq = new ReadOnlySequence<byte>(bytes);
        var reader = new PacketReader(seq);
        var readPacket = new SetContainerSlotPacket();
        readPacket.Read(ref reader);

        Assert.Equal(0, readPacket.WindowId);
        Assert.Equal(12, readPacket.StateId);
        Assert.Equal(36, readPacket.Slot);
        Assert.Equal(45, readPacket.ItemId);
        Assert.Equal(16, readPacket.ItemCount);
    }

    [Fact]
    public void UseItemPacketRoundTrips()
    {
        var packet = new UseItemPacket
        {
            Hand = 0,
            Sequence = 5,
            Yaw = 90.0f,
            Pitch = -45.0f
        };
        Assert.Equal(0x3D, packet.PacketId);

        byte[] bytes = WriteToBytes(packet);
        var seq = new ReadOnlySequence<byte>(bytes);
        var reader = new PacketReader(seq);
        var readPacket = new UseItemPacket();
        readPacket.Read(ref reader);

        Assert.Equal(0, readPacket.Hand);
        Assert.Equal(5, readPacket.Sequence);
        Assert.Equal(90.0f, readPacket.Yaw);
        Assert.Equal(-45.0f, readPacket.Pitch);
    }

    [Fact]
    public void ClickContainerPacketRoundTrips()
    {
        var packet = new ClickContainerPacket
        {
            WindowId = 0,
            StateId = 3,
            Slot = 36,
            Button = 0,
            Mode = 0,
            CarriedItemId = 10,
            CarriedItemCount = 1
        };
        packet.ChangedSlots.Add((36, 10, 2));

        Assert.Equal(0x10, packet.PacketId);

        byte[] bytes = WriteToBytes(packet);
        var seq = new ReadOnlySequence<byte>(bytes);
        var reader = new PacketReader(seq);
        var readPacket = new ClickContainerPacket();
        readPacket.Read(ref reader);

        Assert.Equal(0, readPacket.WindowId);
        Assert.Equal(3, readPacket.StateId);
        Assert.Equal(36, readPacket.Slot);
        Assert.Equal(0, readPacket.Button);
        Assert.Equal(0, readPacket.Mode);
        Assert.Single(readPacket.ChangedSlots);
        Assert.Equal(36, readPacket.ChangedSlots[0].Slot);
        Assert.Equal(10, readPacket.ChangedSlots[0].ItemId);
        Assert.Equal(2, readPacket.ChangedSlots[0].ItemCount);
        Assert.Equal(10, readPacket.CarriedItemId);
        Assert.Equal(1, readPacket.CarriedItemCount);
    }
}
