using System;

namespace Aurora.Protocol.Play;

public class EntityTeleportPacket : IPacket
{
    public int PacketId => 0x77; // Clientbound in 1.21.4

    public int EntityId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public bool OnGround { get; set; }

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(EntityId);
        writer.WriteDouble(X);
        writer.WriteDouble(Y);
        writer.WriteDouble(Z);
        writer.WriteByte((byte)(Yaw * 256.0f / 360.0f));
        writer.WriteByte((byte)(Pitch * 256.0f / 360.0f));
        writer.WriteBool(OnGround);
    }
}

public class EntityHeadRotationPacket : IPacket
{
    public int PacketId => 0x4D; // Clientbound in 1.21.4

    public int EntityId { get; set; }
    public float HeadYaw { get; set; }

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(EntityId);
        writer.WriteByte((byte)(HeadYaw * 256.0f / 360.0f));
    }
}
