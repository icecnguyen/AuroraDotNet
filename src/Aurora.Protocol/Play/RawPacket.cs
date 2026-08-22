namespace Aurora.Protocol.Play;

public class RawPacket : IPacket
{
    private readonly int _packetId;
    private readonly byte[] _payload;

    public int PacketId => _packetId;

    public RawPacket(int packetId, byte[] payload)
    {
        _packetId = packetId;
        _payload = payload;
    }

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        var span = writer.Writer.GetSpan(_payload.Length);
        _payload.CopyTo(span);
        writer.Writer.Advance(_payload.Length);
    }
}
