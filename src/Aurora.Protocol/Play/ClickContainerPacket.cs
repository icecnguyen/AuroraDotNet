using System.Collections.ObjectModel;

namespace Aurora.Protocol.Play;

/// <summary>
/// Serverbound 0x10: click_container
/// Sent when the player clicks on a slot in an open inventory/container window.
/// </summary>
public sealed class ClickContainerPacket : IPacket
{
    public int PacketId => 0x10;

    public int WindowId { get; set; }
    public int StateId { get; set; }
    public short Slot { get; set; }
    public byte Button { get; set; }
    public int Mode { get; set; }

    public Collection<(short Slot, int ItemId, int ItemCount)> ChangedSlots { get; } = new();
    public int CarriedItemId { get; set; }
    public int CarriedItemCount { get; set; }

    public void Read(ref PacketReader reader)
    {
        WindowId = reader.ReadVarInt();
        StateId = reader.ReadVarInt();
        Slot = reader.ReadShort();
        Button = reader.ReadByte();
        Mode = reader.ReadVarInt();

        int length = reader.ReadVarInt();
        ChangedSlots.Clear();
        for (int i = 0; i < length; i++)
        {
            short slotId = reader.ReadShort();
            int count = reader.ReadVarInt();
            int itemId = 0;
            if (count > 0)
            {
                itemId = reader.ReadVarInt();
                _ = reader.ReadVarInt();
                _ = reader.ReadVarInt();
            }
            ChangedSlots.Add((slotId, itemId, count));
        }

        int carriedCount = reader.ReadVarInt();
        if (carriedCount > 0)
        {
            CarriedItemCount = carriedCount;
            CarriedItemId = reader.ReadVarInt();
            _ = reader.ReadVarInt();
            _ = reader.ReadVarInt();
        }
        else
        {
            CarriedItemCount = 0;
            CarriedItemId = 0;
        }
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(WindowId);
        writer.WriteVarInt(StateId);
        writer.WriteShort(Slot);
        writer.WriteByte(Button);
        writer.WriteVarInt(Mode);

        writer.WriteVarInt(ChangedSlots.Count);
        foreach (var changed in ChangedSlots)
        {
            writer.WriteShort(changed.Slot);
            if (changed.ItemCount <= 0 || changed.ItemId <= 0)
            {
                writer.WriteVarInt(0);
            }
            else
            {
                writer.WriteVarInt(changed.ItemCount);
                writer.WriteVarInt(changed.ItemId);
                writer.WriteVarInt(0);
                writer.WriteVarInt(0);
            }
        }

        if (CarriedItemCount <= 0 || CarriedItemId <= 0)
        {
            writer.WriteVarInt(0);
        }
        else
        {
            writer.WriteVarInt(CarriedItemCount);
            writer.WriteVarInt(CarriedItemId);
            writer.WriteVarInt(0);
            writer.WriteVarInt(0);
        }
    }
}
