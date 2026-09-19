using System;

namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound 0x47: remove_entities
/// Removes physical entities from client rendering.
/// </summary>
public sealed class RemoveEntitiesPacket : IPacket
{
    public int PacketId => 0x47;

#pragma warning disable CA1819
    public int[] EntityIds { get; set; } = Array.Empty<int>();
#pragma warning restore CA1819

    public RemoveEntitiesPacket() { }

    public RemoveEntitiesPacket(int entityId)
    {
        EntityIds = new[] { entityId };
    }

    public void Read(ref PacketReader reader)
    {
        int count = reader.ReadVarInt();
        EntityIds = new int[count];
        for (int i = 0; i < count; i++)
        {
            EntityIds[i] = reader.ReadVarInt();
        }
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(EntityIds.Length);
        for (int i = 0; i < EntityIds.Length; i++)
        {
            writer.WriteVarInt(EntityIds[i]);
        }
    }
}
