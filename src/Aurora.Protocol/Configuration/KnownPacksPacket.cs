namespace Aurora.Protocol.Configuration;

public class KnownPacksPacket : IPacket
{
    public int PacketId => 0x0E; // 1.21.4 packet_select_known_packs (clientbound)

    public void Read(ref PacketReader reader)
    {
        // Clientbound only
        _ = reader;
    }

    public void Write(ref PacketWriter writer)
    {
        // Write 1 known pack (minecraft:core, version 1.21.4)
        writer.WriteVarInt(1);
        
        writer.WriteString("minecraft");
        writer.WriteString("core");
        writer.WriteString("1.21.4");
    }
}
