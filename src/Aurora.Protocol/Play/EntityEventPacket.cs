namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound 0x1F: entity_event
/// Used to trigger client-side entity statuses/animations (e.g. 2 = hurt, 3 = death).
/// </summary>
public sealed class EntityEventPacket : IPacket
{
    public int PacketId => 0x1F;

    public int EntityId { get; set; }
    public byte EventId { get; set; }

    public void Read(ref PacketReader reader)
    {
        EntityId = reader.ReadInt();
        EventId = reader.ReadByte();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteInt(EntityId);
        writer.WriteByte(EventId);
    }
}
