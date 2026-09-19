namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound 0x3A: player_abilities
/// Flags: bit 0 = Invulnerable, bit 1 = Flying, bit 2 = Allow Flying, bit 3 = Creative Mode / Instant Break.
/// </summary>
public sealed class PlayerAbilitiesPacket : IPacket
{
    public int PacketId => 0x3A;

    public byte Flags { get; set; }
    public float FlyingSpeed { get; set; } = 0.05f;
    public float WalkingSpeed { get; set; } = 0.10f;

    public void Read(ref PacketReader reader)
    {
        Flags = reader.ReadByte();
        FlyingSpeed = reader.ReadFloat();
        WalkingSpeed = reader.ReadFloat();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteByte(Flags);
        writer.WriteFloat(FlyingSpeed);
        writer.WriteFloat(WalkingSpeed);
    }
}
