namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound 0x15: set_container_slot
/// Sets the contents of a slot in a window/container (window 0 = player inventory).
/// </summary>
public sealed class SetContainerSlotPacket : IPacket
{
    public int PacketId => 0x15;

    public int WindowId { get; set; }
    public int StateId { get; set; }
    public short Slot { get; set; }
    public int ItemId { get; set; }
    public int ItemCount { get; set; }

    public void Read(ref PacketReader reader)
    {
        WindowId = reader.ReadVarInt();
        StateId = reader.ReadVarInt();
        Slot = reader.ReadShort();
        int count = reader.ReadVarInt();
        if (count > 0)
        {
            ItemCount = count;
            ItemId = reader.ReadVarInt();
            _ = reader.ReadVarInt(); // added components count
            _ = reader.ReadVarInt(); // removed components count
        }
        else
        {
            ItemCount = 0;
            ItemId = 0;
        }
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(WindowId);
        writer.WriteVarInt(StateId);
        writer.WriteShort(Slot);

        if (ItemId <= 0 || ItemCount <= 0)
        {
            writer.WriteVarInt(0);
        }
        else
        {
            writer.WriteVarInt(ItemCount);
            writer.WriteVarInt(ItemId);
            writer.WriteVarInt(0); // added components
            writer.WriteVarInt(0); // removed components
        }
    }
}
