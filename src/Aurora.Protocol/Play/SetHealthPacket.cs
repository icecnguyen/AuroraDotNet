namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound 0x62: set_health
/// </summary>
public sealed class SetHealthPacket : IPacket
{
    public int PacketId => 0x62;

    public float Health { get; set; } = 20.0f;
    public int Food { get; set; } = 20;
    public float FoodSaturation { get; set; } = 5.0f;

    public void Read(ref PacketReader reader)
    {
        Health = reader.ReadFloat();
        Food = reader.ReadVarInt();
        FoodSaturation = reader.ReadFloat();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteFloat(Health);
        writer.WriteVarInt(Food);
        writer.WriteFloat(FoodSaturation);
    }
}
