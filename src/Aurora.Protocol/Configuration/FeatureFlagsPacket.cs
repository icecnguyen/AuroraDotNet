namespace Aurora.Protocol.Configuration;

public class FeatureFlagsPacket : IPacket
{
    public int PacketId => 0x0C; // 1.21.4 packet_feature_flags (clientbound)

#pragma warning disable CA1819
    public string[] Features { get; set; } = new[] { "minecraft:vanilla" };
#pragma warning restore CA1819

    public void Read(ref PacketReader reader)
    {
        // Clientbound only
        _ = reader;
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(Features.Length);
        foreach (var feature in Features)
        {
            writer.WriteString(feature);
        }
    }
}
