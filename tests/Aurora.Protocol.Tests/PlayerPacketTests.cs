using System;
using System.Buffers;
using Aurora.Protocol;
using Aurora.Protocol.Play;
using Xunit;

namespace Aurora.Protocol.Tests;

public class PlayerPacketTests
{
    private static ReadOnlySequence<byte> WriteToBuffer(IPacket packet)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new PacketWriter(buffer);
        packet.Write(ref writer);
        return new ReadOnlySequence<byte>(buffer.WrittenMemory);
    }

    [Fact]
    public void AnimatePacketRoundtripsCorrectly()
    {
        var packet = new AnimatePacket { EntityId = 1234, Animation = 3 };
        Assert.Equal(0x03, packet.PacketId);

        var data = WriteToBuffer(packet);
        var reader = new PacketReader(data);

        var loaded = new AnimatePacket();
        loaded.Read(ref reader);

        Assert.Equal(1234, loaded.EntityId);
        Assert.Equal(3, loaded.Animation);
    }

    [Fact]
    public void GameStateChangePacketRoundtripsCorrectly()
    {
        var packet = new GameStateChangePacket { Reason = 3, Value = 1.0f };
        Assert.Equal(0x23, packet.PacketId);

        var data = WriteToBuffer(packet);
        var reader = new PacketReader(data);

        var loaded = new GameStateChangePacket();
        loaded.Read(ref reader);

        Assert.Equal(3, loaded.Reason);
        Assert.Equal(1.0f, loaded.Value);
    }

    [Fact]
    public void PlayerAbilitiesPacketRoundtripsCorrectly()
    {
        var packet = new PlayerAbilitiesPacket
        {
            Flags = 0x0F,
            FlyingSpeed = 0.08f,
            WalkingSpeed = 0.15f
        };
        Assert.Equal(0x3A, packet.PacketId);

        var data = WriteToBuffer(packet);
        var reader = new PacketReader(data);

        var loaded = new PlayerAbilitiesPacket();
        loaded.Read(ref reader);

        Assert.Equal(0x0F, loaded.Flags);
        Assert.Equal(0.08f, loaded.FlyingSpeed, 0.001f);
        Assert.Equal(0.15f, loaded.WalkingSpeed, 0.001f);
    }

    [Fact]
    public void SetEquipmentPacketRoundtripsCorrectly()
    {
        var packet = new SetEquipmentPacket
        {
            EntityId = 55,
            Slot = 0,
            ItemId = 777,
            ItemCount = 32
        };
        Assert.Equal(0x60, packet.PacketId);

        var data = WriteToBuffer(packet);
        var reader = new PacketReader(data);

        var loaded = new SetEquipmentPacket();
        loaded.Read(ref reader);

        Assert.Equal(55, loaded.EntityId);
        Assert.Equal(0, loaded.Slot);
        Assert.Equal(777, loaded.ItemId);
        Assert.Equal(32, loaded.ItemCount);
    }

    [Fact]
    public void SetHealthPacketRoundtripsCorrectly()
    {
        var packet = new SetHealthPacket
        {
            Health = 15.5f,
            Food = 18,
            FoodSaturation = 4.0f
        };
        Assert.Equal(0x62, packet.PacketId);

        var data = WriteToBuffer(packet);
        var reader = new PacketReader(data);

        var loaded = new SetHealthPacket();
        loaded.Read(ref reader);

        Assert.Equal(15.5f, loaded.Health, 0.01f);
        Assert.Equal(18, loaded.Food);
        Assert.Equal(4.0f, loaded.FoodSaturation, 0.01f);
    }

    [Fact]
    public void RemoveEntitiesPacketRoundtripsCorrectly()
    {
        var packet = new RemoveEntitiesPacket { EntityIds = new[] { 101, 102, 103 } };
        Assert.Equal(0x47, packet.PacketId);

        var data = WriteToBuffer(packet);
        var reader = new PacketReader(data);

        var loaded = new RemoveEntitiesPacket();
        loaded.Read(ref reader);

        Assert.Equal(3, loaded.EntityIds.Length);
        Assert.Equal(101, loaded.EntityIds[0]);
        Assert.Equal(102, loaded.EntityIds[1]);
        Assert.Equal(103, loaded.EntityIds[2]);
    }

    [Fact]
    public void PlayerRemovePacketRoundtripsCorrectly()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var packet = new PlayerRemovePacket { PlayerUuids = new[] { id1, id2 } };
        Assert.Equal(0x3F, packet.PacketId);

        var data = WriteToBuffer(packet);
        var reader = new PacketReader(data);

        var loaded = new PlayerRemovePacket();
        loaded.Read(ref reader);

        Assert.Equal(2, loaded.PlayerUuids.Length);
        Assert.Equal(id1, loaded.PlayerUuids[0]);
        Assert.Equal(id2, loaded.PlayerUuids[1]);
    }

    [Fact]
    public void ServerboundPlayerPacketsRoundtripCorrectly()
    {
        // Swing
        var swing = new SwingArmServerboundPacket { Hand = 1 };
        var swingData = WriteToBuffer(swing);
        var swingReader = new PacketReader(swingData);
        var loadedSwing = new SwingArmServerboundPacket();
        loadedSwing.Read(ref swingReader);
        Assert.Equal(1, loadedSwing.Hand);

        // Player Command (Sneak)
        var pCmd = new PlayerCommandServerboundPacket { EntityId = 99, ActionId = 3, JumpBoost = 0 };
        var pCmdData = WriteToBuffer(pCmd);
        var pCmdReader = new PacketReader(pCmdData);
        var loadedPCmd = new PlayerCommandServerboundPacket();
        loadedPCmd.Read(ref pCmdReader);
        Assert.Equal(99, loadedPCmd.EntityId);
        Assert.Equal(3, loadedPCmd.ActionId);

        // Client Command (Respawn)
        var cCmd = new ClientCommandServerboundPacket { ActionId = 0 };
        var cCmdData = WriteToBuffer(cCmd);
        var cCmdReader = new PacketReader(cCmdData);
        var loadedCCmd = new ClientCommandServerboundPacket();
        loadedCCmd.Read(ref cCmdReader);
        Assert.Equal(0, loadedCCmd.ActionId);

        // Player Abilities
        var ab = new PlayerAbilitiesServerboundPacket { Flags = 0x02 };
        var abData = WriteToBuffer(ab);
        var abReader = new PacketReader(abData);
        var loadedAb = new PlayerAbilitiesServerboundPacket();
        loadedAb.Read(ref abReader);
        Assert.Equal(0x02, loadedAb.Flags);
    }
}
