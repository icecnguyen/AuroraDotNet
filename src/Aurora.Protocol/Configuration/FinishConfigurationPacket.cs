namespace Aurora.Protocol.Configuration;

public class FinishConfigurationPacket : IPacket
{
#pragma warning disable CA1822
    public int PacketId => 0x03; // 1.21.4 Finish Configuration is 0x03

    public void Read(ref PacketReader reader)
    {
        // Clientbound
        _ = reader;
    }

    public void Write(ref PacketWriter writer)
    {
        // Empty packet
        _ = writer;
    }
#pragma warning restore CA1822
}
