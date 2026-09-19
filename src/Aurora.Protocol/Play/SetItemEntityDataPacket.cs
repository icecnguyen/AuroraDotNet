namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound 0x5D: Updates metadata specifically for an ItemEntity (display item and stack size).
/// </summary>
public sealed class SetItemEntityDataPacket : IPacket
{
    public int PacketId => 0x5D; // Clientbound 1.21.4

    public int EntityId { get; set; }
    public int ItemId { get; set; }
    public int ItemCount { get; set; } = 1;

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(EntityId);

        // Index 8: DATA_ITEM (Type 7 = ITEM_STACK in 1.21.4)
        writer.WriteByte(8);
        writer.WriteVarInt(7);

        if (ItemId <= 0 || ItemCount <= 0)
        {
            writer.WriteVarInt(0); // Empty stack
        }
        else
        {
            writer.WriteVarInt(ItemCount);
            writer.WriteVarInt(ItemId);
            writer.WriteVarInt(0); // added components
            writer.WriteVarInt(0); // removed components
        }

        // Terminator byte
        writer.WriteByte(0xFF);
    }
}
