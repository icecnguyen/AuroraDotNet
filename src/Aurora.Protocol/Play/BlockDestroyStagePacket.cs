namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound 0x06: Updates the visual breaking progress (cracks) of a block for all clients.
/// </summary>
public sealed class BlockDestroyStagePacket : IPacket
{
    public int PacketId => 0x06; // Clientbound 1.21.4

    public int EntityId { get; set; }
    public long Location { get; set; }
    public sbyte DestroyStage { get; set; } // 0-9 for stages, -1 to remove cracks

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(EntityId);
        writer.WriteLong(Location);
        writer.WriteByte((byte)DestroyStage);
    }
}
