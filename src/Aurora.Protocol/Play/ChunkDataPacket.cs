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
        
        // Light Data
        // SkyLightMask (Array of Long)
        writer.WriteVarInt(0); 
        // BlockLightMask (Array of Long)
        writer.WriteVarInt(0);
        // EmptySkyLightMask (Array of Long)
        writer.WriteVarInt(0);
        // EmptyBlockLightMask (Array of Long)
        writer.WriteVarInt(0);
        
        // SkyLight Arrays
        writer.WriteVarInt(0);
        
        // BlockLight Arrays
        writer.WriteVarInt(0);
    }
}
