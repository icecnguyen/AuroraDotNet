namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound 0x23: game_event (Game State Change)
/// Reason 3: Change GameMode (Value: 0.0f = Survival, 1.0f = Creative, 2.0f = Adventure, 3.0f = Spectator)
/// </summary>
public sealed class GameStateChangePacket : IPacket
{
    public int PacketId => 0x23;

    public byte Reason { get; set; } = 3; // Change game mode
    public float Value { get; set; }

    public void Read(ref PacketReader reader)
    {
        Reason = reader.ReadByte();
        Value = reader.ReadFloat();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteByte(Reason);
        writer.WriteFloat(Value);
    }
}
