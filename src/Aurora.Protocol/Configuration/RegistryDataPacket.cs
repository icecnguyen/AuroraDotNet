namespace Aurora.Protocol.Configuration;

public class RegistryDataPacket : IPacket
{
#pragma warning disable CA1822
    public int PacketId => 0x07; // 1.21.4 Registry Data is 0x07

#pragma warning restore CA1822

    public string RegistryId { get; set; } = string.Empty;
#pragma warning disable CA1819
    public byte[] NbtData { get; set; } = Array.Empty<byte>();
#pragma warning restore CA1819

#pragma warning disable CA1822
    public void Read(ref PacketReader reader)
    {
        // Clientbound only
        _ = reader;
    }
#pragma warning restore CA1822

    public void Write(ref PacketWriter writer)
    {
        writer.WriteString(RegistryId);
        writer.WriteVarInt(1); // 1 entry
        writer.WriteString("minecraft:dummy");
        writer.WriteBool(true); // Has data
        
        // Write the raw NBT data
        var span = writer.Writer.GetSpan(NbtData.Length);
        NbtData.CopyTo(span);
        writer.Writer.Advance(NbtData.Length);
    }
}
