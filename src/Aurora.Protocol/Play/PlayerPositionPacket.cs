using System;

namespace Aurora.Protocol.Play;

public class PlayerPositionPacket : IPacket
{
    public int PacketId => 0x42; // 1.21.4 packet_position

    public int TeleportId { get; set; } = 1;
    public double X { get; set; } = 0.5;
    public double Y { get; set; } = 70.0;
    public double Z { get; set; } = 0.5;
    public double DX { get; set; }
    public double DY { get; set; }
    public double DZ { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public int Flags { get; set; } // Absolute for all

    public void Read(ref PacketReader reader)
    {
        _ = reader;
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(TeleportId);
        writer.WriteDouble(X);
        writer.WriteDouble(Y);
        writer.WriteDouble(Z);
        writer.WriteDouble(DX);
        writer.WriteDouble(DY);
        writer.WriteDouble(DZ);
        writer.WriteFloat(Yaw);
        writer.WriteFloat(Pitch);
        writer.WriteInt(Flags);
    }
}
