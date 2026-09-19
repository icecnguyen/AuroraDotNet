using System.Text;

namespace Aurora.Protocol.Play;

public class SystemChatMessagePacket : IPacket
{
    public int PacketId => 0x73; // Clientbound

    public string Content { get; set; } = string.Empty;
    public bool Overlay { get; set; }

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        byte[] utf8Bytes = Encoding.UTF8.GetBytes(Content ?? string.Empty);

        // In Minecraft 1.20.3+ / 1.21.4 (Protocol 768), Text Components in System Chat are anonymous Network NBT.
        // For plain text (including '§' color codes), an unnamed TAG_String (ID = 8) is used:
        // 1. Type ID (1 byte) = 8
        // 2. String Length (2 bytes Big-Endian ushort)
        // 3. String UTF-8 payload bytes
        writer.WriteByte(8); // TAG_String
        writer.WriteByte((byte)(utf8Bytes.Length >> 8));
        writer.WriteByte((byte)(utf8Bytes.Length & 0xFF));
        
        var span = writer.Writer.GetSpan(utf8Bytes.Length);
        utf8Bytes.CopyTo(span);
        writer.Writer.Advance(utf8Bytes.Length);

        writer.WriteBool(Overlay);
    }
}
