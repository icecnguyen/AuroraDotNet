namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound 0x5D: set_entity_data (Entity Metadata)
/// Synchronizes visual flags (sneaking, sprinting) and pose.
/// </summary>
public sealed class SetEntityDataPacket : IPacket
{
    public int PacketId => 0x5D;

    public int EntityId { get; set; }
    public bool IsSneaking { get; set; }
    public bool IsSprinting { get; set; }

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(EntityId);

        byte flags = 0;
        if (IsSneaking) flags |= 0x02; // Crouching
        if (IsSprinting) flags |= 0x08; // Sprinting

        // Index 0: Base entity flags (Type 0 = byte)
        writer.WriteByte(0);
        writer.WriteVarInt(0);
        writer.WriteByte(flags);

        // Index 6: Pose (Type 21 = pose, VarInt: 0 = Standing, 5 = Crouching)
        writer.WriteByte(6);
        writer.WriteVarInt(21);
        writer.WriteVarInt(IsSneaking ? 5 : 0);

        // Terminate metadata loop
        writer.WriteByte(0xFF);
    }
}
