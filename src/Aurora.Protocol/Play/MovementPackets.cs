using System;

namespace Aurora.Protocol.Play;

public class SetPlayerPositionPacket : IPacket
{
    public int PacketId => 0x1C; // Serverbound

    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public bool OnGround { get; set; }

    public void Read(ref PacketReader reader)
    {
        X = reader.ReadDouble();
        Y = reader.ReadDouble();
        Z = reader.ReadDouble();
        OnGround = reader.ReadBool();
    }

    public void Write(ref PacketWriter writer) { }
}

public class SetPlayerPositionAndRotationPacket : IPacket
{
    public int PacketId => 0x1D; // Serverbound

    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public bool OnGround { get; set; }

    public void Read(ref PacketReader reader)
    {
        X = reader.ReadDouble();
        Y = reader.ReadDouble();
        Z = reader.ReadDouble();
        Yaw = reader.ReadFloat();
        Pitch = reader.ReadFloat();
        OnGround = reader.ReadBool();
    }

    public void Write(ref PacketWriter writer) { }
}

public class SetPlayerRotationPacket : IPacket
{
    public int PacketId => 0x1E; // Serverbound

    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public bool OnGround { get; set; }

    public void Read(ref PacketReader reader)
    {
        Yaw = reader.ReadFloat();
        Pitch = reader.ReadFloat();
        OnGround = reader.ReadBool();
    }

    public void Write(ref PacketWriter writer) { }
}
