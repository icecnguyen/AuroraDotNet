using System;

namespace Aurora.Protocol.Play;

public class PlayerInfoUpdatePacket : IPacket
{
    public int PacketId => 0x40; // 1.21.4 packet_player_info (protocol_id 64 = 0x40)

    public byte Actions { get; set; }
#pragma warning disable CA1819
    public PlayerInfoEntry[] Entries { get; set; } = Array.Empty<PlayerInfoEntry>();
#pragma warning restore CA1819

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteByte(Actions);
        writer.WriteVarInt(Entries.Length);
        
        foreach (var entry in Entries)
        {
            writer.WriteUUID(entry.UUID);
            
            // Bit 0: add_player
            if ((Actions & 0x01) != 0)
            {
                writer.WriteString(entry.Name);
                writer.WriteVarInt(entry.Properties?.Length ?? 0);
                if (entry.Properties != null)
                {
                    foreach (var prop in entry.Properties)
                    {
                        writer.WriteString(prop.Name);
                        writer.WriteString(prop.Value);
                        writer.WriteBool(prop.IsSigned);
                        if (prop.IsSigned) writer.WriteString(prop.Signature ?? "");
                    }
                }
            }
            
            // Bit 1: initialize_chat (we just say false for no chat session)
            if ((Actions & 0x02) != 0)
            {
                writer.WriteBool(false); 
            }
            
            // Bit 2: update_game_mode
            if ((Actions & 0x04) != 0)
            {
                writer.WriteVarInt(entry.GameMode);
            }
            
            // Bit 3: update_listed
            if ((Actions & 0x08) != 0)
            {
                writer.WriteBool(entry.Listed);
            }
            
            // Bit 4: update_latency
            if ((Actions & 0x10) != 0)
            {
                writer.WriteVarInt(entry.Ping);
            }
            
            // Bit 5: update_display_name
            if ((Actions & 0x20) != 0)
            {
                writer.WriteBool(entry.HasDisplayName);
                if (entry.HasDisplayName && entry.DisplayNameJson != null)
                {
                    // Text component as NBT
                    writer.WriteByte(8); // TAG_String
                    byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(entry.DisplayNameJson);
                    writer.WriteByte((byte)(utf8Bytes.Length >> 8));
                    writer.WriteByte((byte)(utf8Bytes.Length & 0xFF));
                    var span = writer.Writer.GetSpan(utf8Bytes.Length);
                    utf8Bytes.CopyTo(span);
                    writer.Writer.Advance(utf8Bytes.Length);
                }
            }
            
            // Bit 6: update_hat
            if ((Actions & 0x40) != 0)
            {
                writer.WriteBool(false); // showHat
            }

            // Bit 7: update_list_order
            if ((Actions & 0x80) != 0)
            {
                writer.WriteVarInt(0); // default list order
            }
        }
    }
}

public class PlayerInfoEntry
{
    public Guid UUID { get; set; }
    public string Name { get; set; } = string.Empty;
#pragma warning disable CA1819
    public ProfileProperty[]? Properties { get; set; }
#pragma warning restore CA1819
    public int GameMode { get; set; } = 1;
    public bool Listed { get; set; } = true;
    public int Ping { get; set; }
    public bool HasDisplayName { get; set; }
    public string? DisplayNameJson { get; set; }
}

public class ProfileProperty
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsSigned { get; set; }
    public string? Signature { get; set; }
}
