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
        string json = $"{{\"text\":\"{Content}\"}}";
        byte[] utf8Bytes = Encoding.UTF8.GetBytes(json);

        // In 1.21.4, Text Components are NBT. 
        // We write an unnamed TAG_String.
        // TAG_String ID = 8
        writer.WriteByte(8);
        
        // Unnamed: length 0 for name (we might not need name length if it's network NBT, wait!
        // Network NBT (anonymous) skips the name entirely. It just writes the ID, then the payload.
        // Wait, NO. If it's Network NBT, it DOES NOT write the ID for the root if the root type is known, OR it writes just the ID without the name?
        // Actually, Network NBT usually has the ID. Let's write ID (8), then payload (VarInt length? No, String payload uses 2-byte short for length? No, Network NBT uses modified UTF-8, but standard NBT uses 2-byte length. Actually in 1.20.2+ Network NBT uses VarInt? No, wait!
        // To be safe, we can use the simplest NBT encoder. 
        // Let's use standard Java NBT String format:
        // ID (1 byte) = 8
        // String length (ushort) = json length
        // String bytes
        // No! In 1.20.2+ NBT is passed with an unnamed root.
        writer.WriteByte(8); // TAG_String
        // Java NBT strings use DataOutputStream.writeUTF, which is 2-byte length then bytes.
        writer.WriteByte((byte)(utf8Bytes.Length >> 8));
        writer.WriteByte((byte)(utf8Bytes.Length & 0xFF));
        
        var span = writer.Writer.GetSpan(utf8Bytes.Length);
        utf8Bytes.CopyTo(span);
        writer.Writer.Advance(utf8Bytes.Length);

        writer.WriteBool(Overlay);
    }
}
