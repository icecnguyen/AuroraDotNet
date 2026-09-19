namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound 0x03: animate (Entity Animation)
/// Animations: 0 = Swing main arm, 1 = Take damage / hurt, 2 = Leave bed, 3 = Swing offhand, 4 = Critical hit, 5 = Magic critical hit.
/// </summary>
public sealed class AnimatePacket : IPacket
{
    public int PacketId => 0x03;

    public int EntityId { get; set; }
    public byte Animation { get; set; }

    public void Read(ref PacketReader reader)
    {
        EntityId = reader.ReadVarInt();
        Animation = reader.ReadByte();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(EntityId);
        writer.WriteByte(Animation);
    }
}
