namespace Aurora.Protocol.Play;

public class KeepAliveServerboundPacket : IPacket
{
    public int PacketId => 0x1A; // 1.21.4 packet_keep_alive (serverbound)

    public long KeepAliveId { get; set; }

    public void Read(ref PacketReader reader)
    {
        KeepAliveId = reader.ReadLong();
    }

    public void Write(ref PacketWriter writer) { }
}
