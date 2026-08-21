namespace Aurora.Protocol;

public interface IPacket
{
    int PacketId { get; }
    void Read(ref PacketReader reader);
    void Write(ref PacketWriter writer);
}
