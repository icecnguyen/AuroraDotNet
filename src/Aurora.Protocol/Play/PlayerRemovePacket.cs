using System;

namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound 0x3F: player_info_remove
/// Removes players from the client's tab list upon disconnect.
/// </summary>
public sealed class PlayerRemovePacket : IPacket
{
    public int PacketId => 0x3F;

#pragma warning disable CA1819
    public Guid[] PlayerUuids { get; set; } = Array.Empty<Guid>();
#pragma warning restore CA1819

    public PlayerRemovePacket() { }

    public PlayerRemovePacket(Guid uuid)
    {
        PlayerUuids = new[] { uuid };
    }

    public void Read(ref PacketReader reader)
    {
        int count = reader.ReadVarInt();
        PlayerUuids = new Guid[count];
        for (int i = 0; i < count; i++)
        {
            PlayerUuids[i] = reader.ReadUUID();
        }
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(PlayerUuids.Length);
        for (int i = 0; i < PlayerUuids.Length; i++)
        {
            writer.WriteUUID(PlayerUuids[i]);
        }
    }
}
