namespace Aurora.Protocol.Configuration;

public class UpdateTagsPacket : IPacket
{
    public int PacketId => 0x0D; // 1.21.4 packet_tags (clientbound)

#pragma warning disable CA1819
    public byte[] Payload { get; set; } = System.Array.Empty<byte>();
#pragma warning restore CA1819

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        var span = writer.Writer.GetSpan(Payload.Length);
        Payload.CopyTo(span);
        writer.Writer.Advance(Payload.Length);
    }
}
