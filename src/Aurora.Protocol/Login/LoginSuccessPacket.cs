using System;

namespace Aurora.Protocol.Login;

public sealed class LoginSuccessPacket : IPacket
{
    public int PacketId => 0x02;
    
    public Guid Uuid { get; set; }
    public string Username { get; set; } = string.Empty;

    public void Read(ref PacketReader reader)
    {
        Uuid = reader.ReadUUID();
        Username = reader.ReadString(16);
        
        int propertyCount = reader.ReadVarInt();
        // Skip properties for now
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteUUID(Uuid);
        writer.WriteString(Username);
        writer.WriteVarInt(0); // 0 Properties
    }
}
