using System;

namespace Aurora.Protocol.Play;

public class PlayerActionPacket : IPacket
{
    public int PacketId => 0x27; // Serverbound

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
    public int PacketId => 0x3C; // Serverbound

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
    public int PacketId => 0x09; // Clientbound

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
