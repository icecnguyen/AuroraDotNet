namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound 0x60: set_equipment
/// Slots: 0 = Main hand, 1 = Off hand, 2 = Boots, 3 = Leggings, 4 = Chestplate, 5 = Helmet.
/// </summary>
public sealed class SetEquipmentPacket : IPacket
{
    public int PacketId => 0x60;

    public int EntityId { get; set; }
    public byte Slot { get; set; } // 0 = Main hand
    public int ItemId { get; set; }
    public byte ItemCount { get; set; }

    public void Read(ref PacketReader reader)
    {
        EntityId = reader.ReadVarInt();
        byte slotRaw = reader.ReadByte();
        Slot = (byte)(slotRaw & 0x7F);

        int count = reader.ReadVarInt();
        if (count > 0)
        {
            ItemId = reader.ReadVarInt();
            _ = reader.ReadVarInt(); // addedComponents count
            _ = reader.ReadVarInt(); // removedComponents count
            ItemCount = (byte)count;
        }
        else
        {
            ItemId = 0;
            ItemCount = 0;
        }
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(EntityId);
        // Single slot entry: top bit 0 indicates this is the last entry
        writer.WriteByte((byte)(Slot & 0x7F));

        if (ItemId <= 0 || ItemCount <= 0)
        {
            writer.WriteVarInt(0);
        }
        else
        {
            writer.WriteVarInt(ItemCount);
            writer.WriteVarInt(ItemId);
            writer.WriteVarInt(0); // addedComponents
            writer.WriteVarInt(0); // removedComponents
        }
    }
}
