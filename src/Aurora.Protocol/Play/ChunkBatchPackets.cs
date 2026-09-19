namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound 0x0D: Signals the beginning of a chunk batch in 1.21.4 (Protocol 768).
/// </summary>
public sealed class ChunkBatchStartPacket : IPacket
{
    public int PacketId => 0x0D; // Clientbound 1.21.4

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer) { }
}

/// <summary>
/// Clientbound 0x0C: Signals that the server has finished sending a batch of chunks.
/// </summary>
public sealed class ChunkBatchFinishedPacket : IPacket
{
    public int PacketId => 0x0C; // Clientbound 1.21.4

    public int BatchSize { get; set; }

    public void Read(ref PacketReader reader)
    {
        BatchSize = reader.ReadVarInt();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteVarInt(BatchSize);
    }
}

/// <summary>
/// Serverbound 0x08: Client acknowledges receipt of a chunk batch.
/// </summary>
public sealed class ChunkBatchReceivedPacket : IPacket
{
    public int PacketId => 0x08; // Serverbound 1.21.4

    public float DesiredChunksPerTick { get; set; }

    public void Read(ref PacketReader reader)
    {
        DesiredChunksPerTick = reader.ReadFloat();
    }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteFloat(DesiredChunksPerTick);
    }
}
