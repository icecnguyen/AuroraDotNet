using System;

namespace Aurora.Protocol.Play;

public class SpawnEntityPacket : IPacket
{
    public int PacketId => 0x01; // Clientbound in 1.21.4

    public int EntityId { get; set; }
    public Guid EntityUUID { get; set; }
    public int Type { get; set; } = 147; // 147 for Player in 1.21.4
    
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    
    public float Pitch { get; set; }
    public float Yaw { get; set; }
    public float HeadYaw { get; set; }
    
    public int Data { get; set; }
    public short VelocityX { get; set; }
    public short VelocityY { get; set; }
    public short VelocityZ { get; set; }

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(EntityId);
        writer.WriteUUID(EntityUUID);
        writer.WriteVarInt(Type);
        
        writer.WriteDouble(X);
        writer.WriteDouble(Y);
        writer.WriteDouble(Z);
        
        // Angles are represented as (angle * 256.0F / 360.0F) byte
        writer.WriteByte((byte)(Pitch * 256.0f / 360.0f));
        writer.WriteByte((byte)(Yaw * 256.0f / 360.0f));
        writer.WriteByte((byte)(HeadYaw * 256.0f / 360.0f));
        
        writer.WriteVarInt(Data);
        
        // Velocity (shorts)
        writer.WriteUShort((ushort)VelocityX);
        writer.WriteUShort((ushort)VelocityY);
        writer.WriteUShort((ushort)VelocityZ);
    }
}
