namespace Aurora.Protocol.Status;

public sealed class PingRequestPacket : IPacket
{
    public int PacketId => 0x01;
    
    public long Payload { get; set; }

    public void Read(ref PacketReader reader)
    {
        Payload = reader.ReadLong();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteLong(Payload);
    }
}
