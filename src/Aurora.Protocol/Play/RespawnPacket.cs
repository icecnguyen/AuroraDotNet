namespace Aurora.Protocol.Play;

/// <summary>
/// Clientbound 0x4C: respawn
/// Transmitted when player respawns after death or changes dimensions.
/// </summary>
public sealed class RespawnPacket : IPacket
{
    public int PacketId => 0x4C;

    // SpawnInfo
    public int Dimension { get; set; }
    public string DimensionName { get; set; } = "minecraft:overworld";
    public long HashedSeed { get; set; }
    public byte GameMode { get; set; }
    public byte PreviousGameMode { get; set; } = 255;
    public bool IsDebug { get; set; }
    public bool IsFlat { get; set; }
    public bool HasDeathLocation { get; set; }
    public int PortalCooldown { get; set; }
    public int SeaLevel { get; set; } = 63;

    public byte CopyMetadata { get; set; } = 1; // 1 = Keep attributes/data

    public void Read(ref PacketReader reader) { }

    public void Write(ref PacketWriter writer)
    {
        // SpawnInfo
        writer.WriteVarInt(Dimension);
        writer.WriteString(DimensionName);
        writer.WriteLong(HashedSeed);
        writer.WriteByte(GameMode);
        writer.WriteByte(PreviousGameMode);
        writer.WriteBool(IsDebug);
        writer.WriteBool(IsFlat);
        writer.WriteBool(HasDeathLocation);
        writer.WriteVarInt(PortalCooldown);
        writer.WriteVarInt(SeaLevel);

        // copyMetadata
        writer.WriteByte(CopyMetadata);
    }
}
