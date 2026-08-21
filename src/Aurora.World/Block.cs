namespace Aurora.World;

/// <summary>
/// Constants for well-known block state IDs (assuming Minecraft 1.20+ format for simplicity).
/// In a real engine, these are loaded from a registry.
/// </summary>
public static class Block
{
    public const ushort Air = 0;
    public const ushort Stone = 1;
    public const ushort GrassBlock = 2;
    public const ushort Dirt = 3;
    public const ushort Bedrock = 4;
}
