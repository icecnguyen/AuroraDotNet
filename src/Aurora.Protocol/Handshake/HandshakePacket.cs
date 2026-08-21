namespace Aurora.Protocol.Handshake;

public sealed class HandshakePacket : IPacket
{
    public int PacketId => 0x00;

    public int ProtocolVersion { get; set; }
    public string ServerAddress { get; set; } = string.Empty;
    public ushort ServerPort { get; set; }
    public ConnectionState NextState { get; set; }

    public void Read(ref PacketReader reader)
    {
        ProtocolVersion = reader.ReadVarInt();
        ServerAddress = reader.ReadString(255);
        ServerPort = reader.ReadUShort();
        NextState = (ConnectionState)reader.ReadVarInt();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(ProtocolVersion);
        writer.WriteString(ServerAddress);
        writer.WriteUShort(ServerPort);
        writer.WriteVarInt((int)NextState);
    }
}
