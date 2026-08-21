namespace Aurora.Protocol.Play;

public class JoinGamePacket : IPacket
{
#pragma warning disable CA1822
    public int PacketId => 0x2C; // 1.21.4 packet_login
#pragma warning restore CA1822


    public int EntityId { get; set; } = 1;
    public bool IsHardcore { get; set; }

    public void Read(ref PacketReader reader)
    {
        _ = reader;
    }

#pragma warning disable CA1819
    public string[] DimensionNames { get; set; } = new[] { "minecraft:overworld" };
#pragma warning restore CA1819
    public int MaxPlayers { get; set; } = 100;
    public int ViewDistance { get; set; } = 8;
    public int SimulationDistance { get; set; } = 8;
    public bool ReducedDebugInfo { get; set; }
    public bool EnableRespawnScreen { get; set; } = true;
    public bool DoLimitedCrafting { get; set; }
    
    // SpawnInfo
    public int Dimension { get; set; } // VarInt index into dimension registry
    public string DimensionName { get; set; } = "minecraft:overworld";
    public long HashedSeed { get; set; }
    public byte GameMode { get; set; } = 1; // Creative
    public byte PreviousGameMode { get; set; } = (byte)255;
    public bool IsDebug { get; set; }
    public bool IsFlat { get; set; } = true;
    public bool HasDeathLocation { get; set; }
    public int PortalCooldown { get; set; }
    public int SeaLevel { get; set; } = 63;
    
    public bool EnforcesSecureChat { get; set; }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteInt(EntityId); // i32
        writer.WriteBool(IsHardcore);
        
        writer.WriteVarInt(DimensionNames.Length);
        foreach (var dim in DimensionNames)
        {
            writer.WriteString(dim);
        }

        writer.WriteVarInt(MaxPlayers);
        writer.WriteVarInt(ViewDistance);
        writer.WriteVarInt(SimulationDistance);
        writer.WriteBool(ReducedDebugInfo);
        writer.WriteBool(EnableRespawnScreen);
        writer.WriteBool(DoLimitedCrafting);
        
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
        
        writer.WriteBool(EnforcesSecureChat);
    }
}
