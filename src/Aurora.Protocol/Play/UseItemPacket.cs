namespace Aurora.Protocol.Play;

/// <summary>
/// Serverbound 0x3D: use_item
/// Transmitted when the player right-clicks with an item in hand (e.g. eating food).
/// </summary>
public sealed class UseItemPacket : IPacket
{
    public int PacketId => 0x3D;

    public int Hand { get; set; }
    public int Sequence { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }

    public void Read(ref PacketReader reader)
    {
        Hand = reader.ReadVarInt();
        Sequence = reader.ReadVarInt();
        Yaw = reader.ReadFloat();
        Pitch = reader.ReadFloat();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(Hand);
        writer.WriteVarInt(Sequence);
        writer.WriteFloat(Yaw);
        writer.WriteFloat(Pitch);
    }
}
