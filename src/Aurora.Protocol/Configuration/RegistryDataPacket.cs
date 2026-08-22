namespace Aurora.Protocol.Configuration;

public class RegistryDataPacket : IPacket
{
    public int PacketId => 0x07;

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
