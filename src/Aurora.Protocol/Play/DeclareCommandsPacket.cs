namespace Aurora.Protocol.Play;

/// <summary>
/// Declares the Brigadier command tree to the client (0x11 Clientbound Play).
/// Informs the client of available commands, arguments, and autocompletion paths.
/// </summary>
public class DeclareCommandsPacket : IPacket
{
    public int PacketId => 0x11; // Clientbound Play

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        // Nodes array:
        // 0: Root (flags 0x00, children: [1, 2, 3, 4, 5, 6, 7, 8, 9])
        // 1: "gamemode" (flags 0x01, children: [10, 11, 12, 13])
        // 2: "gm" (flags 0x01, children: [10, 11, 12, 13])
        // 3: "heal" (flags 0x05, children: [])
        // 4: "kill" (flags 0x05, children: [])
        // 5: "ping" (flags 0x05, children: [])
        // 6: "pos" (flags 0x05, children: [])
        // 7: "help" (flags 0x05, children: [])
        // 8: "clear" (flags 0x05, children: [])
        // 9: "time" (flags 0x01, children: [14])
        // 10: "survival" (flags 0x05, children: [])
        // 11: "creative" (flags 0x05, children: [])
        // 12: "adventure" (flags 0x05, children: [])
        // 13: "spectator" (flags 0x05, children: [])
        // 14: "set" (flags 0x01, children: [15, 16])
        // 15: "day" (flags 0x05, children: [])
        // 16: "night" (flags 0x05, children: [])

        writer.WriteVarInt(17); // 17 total nodes

        // Node 0: Root
        writer.WriteByte(0x00);
        writer.WriteVarInt(9); // 9 children
        writer.WriteVarInt(1);
        writer.WriteVarInt(2);
        writer.WriteVarInt(3);
        writer.WriteVarInt(4);
        writer.WriteVarInt(5);
        writer.WriteVarInt(6);
        writer.WriteVarInt(7);
        writer.WriteVarInt(8);
        writer.WriteVarInt(9);

        // Node 1: "gamemode" (literal, non-executable, has subcommands)
        writer.WriteByte(0x01);
        writer.WriteVarInt(4);
        writer.WriteVarInt(10);
        writer.WriteVarInt(11);
        writer.WriteVarInt(12);
        writer.WriteVarInt(13);
        writer.WriteString("gamemode");

        // Node 2: "gm" (literal, non-executable, has subcommands)
        writer.WriteByte(0x01);
        writer.WriteVarInt(4);
        writer.WriteVarInt(10);
        writer.WriteVarInt(11);
        writer.WriteVarInt(12);
        writer.WriteVarInt(13);
        writer.WriteString("gm");

        // Node 3: "heal" (literal, executable)
        writer.WriteByte(0x05);
        writer.WriteVarInt(0);
        writer.WriteString("heal");

        // Node 4: "kill" (literal, executable)
        writer.WriteByte(0x05);
        writer.WriteVarInt(0);
        writer.WriteString("kill");

        // Node 5: "ping" (literal, executable)
        writer.WriteByte(0x05);
        writer.WriteVarInt(0);
        writer.WriteString("ping");

        // Node 6: "pos" (literal, executable)
        writer.WriteByte(0x05);
        writer.WriteVarInt(0);
        writer.WriteString("pos");

        // Node 7: "help" (literal, executable)
        writer.WriteByte(0x05);
        writer.WriteVarInt(0);
        writer.WriteString("help");

        // Node 8: "clear" (literal, executable)
        writer.WriteByte(0x05);
        writer.WriteVarInt(0);
        writer.WriteString("clear");

        // Node 9: "time" (literal, non-executable)
        writer.WriteByte(0x01);
        writer.WriteVarInt(1);
        writer.WriteVarInt(14);
        writer.WriteString("time");

        // Node 10: "survival" (literal, executable)
        writer.WriteByte(0x05);
        writer.WriteVarInt(0);
        writer.WriteString("survival");

        // Node 11: "creative" (literal, executable)
        writer.WriteByte(0x05);
        writer.WriteVarInt(0);
        writer.WriteString("creative");

        // Node 12: "adventure" (literal, executable)
        writer.WriteByte(0x05);
        writer.WriteVarInt(0);
        writer.WriteString("adventure");

        // Node 13: "spectator" (literal, executable)
        writer.WriteByte(0x05);
        writer.WriteVarInt(0);
        writer.WriteString("spectator");

        // Node 14: "set" (literal, non-executable)
        writer.WriteByte(0x01);
        writer.WriteVarInt(2);
        writer.WriteVarInt(15);
        writer.WriteVarInt(16);
        writer.WriteString("set");

        // Node 15: "day" (literal, executable)
        writer.WriteByte(0x05);
        writer.WriteVarInt(0);
        writer.WriteString("day");

        // Node 16: "night" (literal, executable)
        writer.WriteByte(0x05);
        writer.WriteVarInt(0);
        writer.WriteString("night");

        // Root node index:
        writer.WriteVarInt(0);
    }
}
