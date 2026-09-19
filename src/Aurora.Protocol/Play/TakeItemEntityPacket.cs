namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound 0x76: Plays the animation of an item entity being collected by another entity (e.g. player).
/// </summary>
public sealed class TakeItemEntityPacket : IPacket
{
    public int PacketId => 0x76; // Clientbound 1.21.4

    public int CollectedEntityId { get; set; }
    public int CollectorEntityId { get; set; }
    public int PickupCount { get; set; }

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(CollectedEntityId);
        writer.WriteVarInt(CollectorEntityId);
        writer.WriteVarInt(PickupCount);
    }
}
