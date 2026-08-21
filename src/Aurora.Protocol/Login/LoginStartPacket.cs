using System;

namespace Aurora.Protocol.Login;

public sealed class LoginStartPacket : IPacket
{
    public int PacketId => 0x00;
    
    public string Name { get; set; } = string.Empty;
    public Guid Uuid { get; set; }

    public void Read(ref PacketReader reader)
    {
        Name = reader.ReadString(16);
        Uuid = reader.ReadUUID();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteString(Name);
        writer.WriteUUID(Uuid);
    }
}
