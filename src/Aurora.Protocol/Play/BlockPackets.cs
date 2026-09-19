using System;

namespace Aurora.Protocol.Play;

public class PlayerActionPacket : IPacket
{
    public int PacketId => 0x27; // Serverbound player_action

    public int Status { get; set; }
    public long Location { get; set; }
    public byte Face { get; set; }
    public int Sequence { get; set; }

    public void Read(ref PacketReader reader)
    {
        Status = reader.ReadVarInt();
        Location = reader.ReadLong();
        Face = reader.ReadByte();
        Sequence = reader.ReadVarInt();
    }

    public void Write(ref PacketWriter writer) { }

    public (int X, int Y, int Z) BlockPosition
    {
        get
        {
            long val = Location;
            int x = (int)(val >> 38);
            int y = (int)(val & 0xFFF);
            int z = (int)((val >> 12) & 0x3FFFFFF);

            // Handle sign extension for 26-bit integers (X and Z)
            if (x >= (1 << 25)) x -= (1 << 26);
            if (z >= (1 << 25)) z -= (1 << 26);
            // Handle sign extension for 12-bit integer (Y)
            if (y >= (1 << 11)) y -= (1 << 12);

            return (x, y, z);
        }
    }
}

public class UseItemOnBlockPacket : IPacket
{
    public int PacketId => 0x3C; // Serverbound use_item_on

    public int Hand { get; set; }
    public long Location { get; set; }
    public int Face { get; set; }
    public float CursorX { get; set; }
    public float CursorY { get; set; }
    public float CursorZ { get; set; }
    public bool InsideBlock { get; set; }
    public int Sequence { get; set; }

    public void Read(ref PacketReader reader)
    {
        Hand = reader.ReadVarInt();
        Location = reader.ReadLong();
        Face = reader.ReadVarInt();
        CursorX = reader.ReadFloat();
        CursorY = reader.ReadFloat();
        CursorZ = reader.ReadFloat();
        InsideBlock = reader.ReadBool();
        Sequence = reader.ReadVarInt();
    }

    public void Write(ref PacketWriter writer) { }

    public (int X, int Y, int Z) BlockPosition
    {
        get
        {
            long val = Location;
            int x = (int)(val >> 38);
            int y = (int)(val & 0xFFF);
            int z = (int)((val >> 12) & 0x3FFFFFF);

            if (x >= (1 << 25)) x -= (1 << 26);
            if (z >= (1 << 25)) z -= (1 << 26);
            if (y >= (1 << 11)) y -= (1 << 12);

            return (x, y, z);
        }
    }
}

public class BlockUpdatePacket : IPacket
{
    public int PacketId => 0x09; // Clientbound block_update

    public long Location { get; set; }
    public int BlockStateId { get; set; }

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteLong(Location);
        writer.WriteVarInt(BlockStateId);
    }

    public static long EncodePosition(int x, int y, int z)
    {
        return (((long)(x & 0x3FFFFFF) << 38) | ((long)(z & 0x3FFFFFF) << 12) | ((long)(y & 0xFFF)));
    }
}

public class AcknowledgeBlockChangePacket : IPacket
{
    public int PacketId => 0x05; // Clientbound block_changed_ack

    public int SequenceId { get; set; }

    public void Read(ref PacketReader reader)
    {
        SequenceId = reader.ReadVarInt();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(SequenceId);
    }
}

public class HeldItemSlotPacket : IPacket
{
    public int PacketId => 0x33; // Serverbound set_carried_item

    public short SlotId { get; set; }

    public void Read(ref PacketReader reader)
    {
        SlotId = reader.ReadShort();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteShort(SlotId);
    }
}

public class SetCreativeModeSlotPacket : IPacket
{
    public int PacketId => 0x36; // Serverbound set_creative_slot

    public short Slot { get; set; }
    public int ItemId { get; set; }
    public int ItemCount { get; set; }

    public void Read(ref PacketReader reader)
    {
        Slot = reader.ReadShort();
        ItemCount = reader.ReadVarInt();
        if (ItemCount > 0)
        {
            ItemId = reader.ReadVarInt();
        }
        else
        {
            ItemId = 0;
        }
    }

    public void Write(ref PacketWriter writer) { }
}

public class SwingArmServerboundPacket : IPacket
{
    public int PacketId => 0x3A; // Serverbound swing

    public int Hand { get; set; } // 0 = Main hand, 1 = Offhand

    public void Read(ref PacketReader reader)
    {
        Hand = reader.ReadVarInt();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(Hand);
    }
}

public class PlayerCommandServerboundPacket : IPacket
{
    public int PacketId => 0x28; // Serverbound player_command

    public int EntityId { get; set; }
    public int ActionId { get; set; } // 0 = Start sneak, 1 = Stop sneak, 3 = Start sprint, 4 = Stop sprint
    public int JumpBoost { get; set; }

    public void Read(ref PacketReader reader)
    {
        EntityId = reader.ReadVarInt();
        ActionId = reader.ReadVarInt();
        JumpBoost = reader.ReadVarInt();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(EntityId);
        writer.WriteVarInt(ActionId);
        writer.WriteVarInt(JumpBoost);
    }
}

public class ClientCommandServerboundPacket : IPacket
{
    public int PacketId => 0x0A; // Serverbound client_command

    public int ActionId { get; set; } // 0 = Perform respawn, 1 = Request stats

    public void Read(ref PacketReader reader)
    {
        ActionId = reader.ReadVarInt();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(ActionId);
    }
}

public class PlayerAbilitiesServerboundPacket : IPacket
{
    public int PacketId => 0x26; // Serverbound player_abilities

    public byte Flags { get; set; } // bit 1: is flying

    public void Read(ref PacketReader reader)
    {
        Flags = reader.ReadByte();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteByte(Flags);
    }
}

