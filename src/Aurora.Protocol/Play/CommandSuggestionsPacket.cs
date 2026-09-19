using System;
using System.Collections.Generic;

namespace Aurora.Protocol.Play;

/// <summary>
/// Serverbound packet sent when the client requests command/tab completion (0x0D in 1.21.4).
/// </summary>
public class CommandSuggestionRequestPacket : IPacket
{
    public int PacketId => 0x0D; // Serverbound 1.21.4

    public int TransactionId { get; set; }
    public string Text { get; set; } = string.Empty;

    public void Read(ref PacketReader reader)
    {
        TransactionId = reader.ReadVarInt();
        Text = reader.ReadString();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(TransactionId);
        writer.WriteString(Text);
    }
}

/// <summary>
/// Clientbound packet responding with tab completion / command suggestions (0x10 in 1.21.4).
/// </summary>
public class CommandSuggestionsResponsePacket : IPacket
{
    public int PacketId => 0x10; // Clientbound 1.21.4

    public int TransactionId { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public IReadOnlyList<string> Matches { get; set; } = Array.Empty<string>();

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(TransactionId);
        writer.WriteVarInt(Start);
        writer.WriteVarInt(Length);
        writer.WriteVarInt(Matches.Count);

        for (int i = 0; i < Matches.Count; i++)
        {
            writer.WriteString(Matches[i]);
            writer.WriteBool(false); // Has tooltip = false
        }
    }
}
