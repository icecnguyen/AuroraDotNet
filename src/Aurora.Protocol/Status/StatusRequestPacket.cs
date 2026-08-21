namespace Aurora.Protocol.Status;

public sealed class StatusRequestPacket : IPacket
{
    public int PacketId => 0x00;

    public void Read(ref PacketReader reader)
    {
        // Empty
    }

    public void Write(ref PacketWriter writer)
    {
        // Empty
    }
}
