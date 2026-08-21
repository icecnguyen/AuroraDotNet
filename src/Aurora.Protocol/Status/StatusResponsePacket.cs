namespace Aurora.Protocol.Status;

public sealed class StatusResponsePacket : IPacket
{
    public int PacketId => 0x00;
    
    public string JsonResponse { get; set; } = string.Empty;

    public void Read(ref PacketReader reader)
    {
        JsonResponse = reader.ReadString(32767);
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteString(JsonResponse);
    }
}
