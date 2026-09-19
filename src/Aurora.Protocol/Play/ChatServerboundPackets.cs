using System;
using System.Text;

namespace Aurora.Protocol.Play;

public class ChatMessageServerboundPacket : IPacket
{
    public int PacketId => 0x07; // Serverbound

    public string Message { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public long Salt { get; set; }
    // There are signature and message count fields, we can just skip/read them to avoid stream corruption
    // Actually we just need to read the string which is the first field, and we can ignore the rest for a simple server.
    // Wait, the packet stream is length-prefixed, so as long as we don't read past the end, we don't strictly need to read everything.
    // But it's good practice.

    public void Read(ref PacketReader reader)
    {
        Message = reader.ReadString();
        // Ignoring the rest for now since we rely on the packet length to advance the buffer anyway.
    }

    public void Write(ref PacketWriter writer) { }
}

public class ChatCommandServerboundPacket : IPacket
{
    public int PacketId => 0x05; // Serverbound

    public string Command { get; set; } = string.Empty;

    public void Read(ref PacketReader reader)
    {
        Command = reader.ReadString();
        // Ignoring the rest of the fields (timestamp, salt, arguments, signed etc.)
    }

    public void Write(ref PacketWriter writer) { }
}

public class ChatCommandSignedServerboundPacket : IPacket
{
    public int PacketId => 0x06; // Serverbound 1.21.4

    public string Command { get; set; } = string.Empty;

    public void Read(ref PacketReader reader)
    {
        Command = reader.ReadString();
        // Ignoring cryptographic signatures, timestamp, salt, argument signatures, and checksum
    }

    public void Write(ref PacketWriter writer) { }
}
