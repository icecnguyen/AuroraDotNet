using System.IO;

namespace Aurora.Protocol.Play;

public class ChunkDataPacket : IPacket
{
    public int PacketId => 0x28; // 1.21.4 packet_map_chunk

    public int X { get; set; }
    public int Z { get; set; }
#pragma warning disable CA1819
    public byte[] HeightmapsNbt { get; set; } = new byte[] { 0x0A, 0x00 }; // Empty anonymous NBT compound by default
    public byte[] ChunkData { get; set; } = System.Array.Empty<byte>();
#pragma warning restore CA1819
    
    // We leave BlockEntities empty for flat world
    // We also leave Light data mostly empty or zeroed, client will recalculate or ignore

#pragma warning disable CA1819
    public byte[] LightData { get; set; } = System.Array.Empty<byte>();
#pragma warning restore CA1819

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteInt(X);
        writer.WriteInt(Z);
        
        // Heightmaps NBT
        var span = writer.Writer.GetSpan(HeightmapsNbt.Length);
        HeightmapsNbt.CopyTo(span);
        writer.Writer.Advance(HeightmapsNbt.Length);
        
        // Chunk Data length + data
        writer.WriteVarInt(ChunkData.Length);
        span = writer.Writer.GetSpan(ChunkData.Length);
        ChunkData.CopyTo(span);
        writer.Writer.Advance(ChunkData.Length);
        
        // Block Entities
        writer.WriteVarInt(0);
        
        if (LightData.Length == 0)
        {
            // Fallback for empty/unloaded chunks
            writer.WriteVarInt(0); writer.WriteVarInt(0); writer.WriteVarInt(0); writer.WriteVarInt(0);
            writer.WriteVarInt(0); writer.WriteVarInt(0);
            return;
        }

        // Write pre-serialized Light Data
        var lightSpan = writer.Writer.GetSpan(LightData.Length);
        LightData.CopyTo(lightSpan);
        writer.Writer.Advance(LightData.Length);
    }
}
