namespace Aurora.Protocol.Play;

public class KeepAliveClientboundPacket : IPacket
{
    public int PacketId => 0x27; // 1.21.4 packet_keep_alive (clientbound)

    public long KeepAliveId { get; set; }

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteLong(KeepAliveId);
    }
}
