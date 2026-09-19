namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound packet that synchronizes world time and day/night cycle (0x6B in 1.21.4).
/// </summary>
public class UpdateTimePacket : IPacket
{
    public int PacketId => 0x6B; // Clientbound 1.21.4

    /// <summary>
    /// Total ticks since world creation.
    /// </summary>
    public long WorldAge { get; set; }

    /// <summary>
    /// Time of day in ticks (0 = 6:00, 1000 = 7:00, 6000 = 12:00, 12000 = 18:00, 13000 = 19:00 / night, 18000 = midnight).
    /// </summary>
    public long TimeOfDay { get; set; }

    /// <summary>
    /// If false, the sun/moon stops moving. If true, time advances normally.
    /// </summary>
    public bool IsIncreasing { get; set; } = true;

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteLong(WorldAge);
        writer.WriteLong(TimeOfDay);
        writer.WriteBool(IsIncreasing);
    }
}
