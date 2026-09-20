using System;
using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Aurora.Core.Configuration;

/// <summary>
/// Represents the server configuration loaded from server.properties according to Vanilla Minecraft specifications.
/// </summary>
public class ServerConfiguration
{
    public int ServerPort { get; set; } = 25565;

    public int Port
    {
        get => ServerPort;
        set => ServerPort = value;
    }

    public string ServerIp { get; set; } = string.Empty;

    public string LevelName { get; set; } = "world";

    public string WorldName
    {
        get => LevelName;
        set => LevelName = value;
    }

    public string LevelSeed { get; set; } = string.Empty;

    public string GameMode { get; set; } = "survival";

    public string Difficulty { get; set; } = "normal";

    public int MaxPlayers { get; set; } = 20;

    public int ViewDistance { get; set; } = 8;

    public int SimulationDistance { get; set; } = 8;

    public int WorkerCount { get; set; } = 4;

    public string Motd { get; set; } = "A Minecraft Server powered by AuroraDotNet";

    public bool Pvp { get; set; } = true;

    public bool AllowFlight { get; set; }

    public bool OnlineMode { get; set; }

    public bool WhiteList { get; set; }

    /// <summary>
    /// The resolved 64-bit seed used by world generator.
    /// </summary>
    public long ResolvedSeed { get; set; } = 12345L;

    /// <summary>
    /// The SHA-256 hashed 64-bit seed sent to client in JoinGame and Respawn packets.
    /// </summary>
    public long HashedSeed { get; set; }

    /// <summary>
    /// Calculates the Java string hashCode for non-numeric seeds (standard Minecraft algorithm).
    /// </summary>
    public static int JavaStringHash(string s)
    {
        ArgumentNullException.ThrowIfNull(s);
        int result = 0;
        foreach (char c in s)
        {
            result = unchecked(31 * result + c);
        }
        return result;
    }

    /// <summary>
    /// Hashes a 64-bit seed into a 64-bit hashed seed using SHA-256 (standard Minecraft BiomeAccess algorithm).
    /// </summary>
    public static long HashSeed(long seed)
    {
        Span<byte> seedBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(seedBytes, seed);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(seedBytes, hash);
        return BinaryPrimitives.ReadInt64LittleEndian(hash[..8]);
    }

    private static long GenerateRandomSeed()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        return BinaryPrimitives.ReadInt64LittleEndian(bytes);
    }

    /// <summary>
    /// Parses a seed string into a 64-bit seed. If numeric, parses directly; if text, uses JavaStringHash; if empty, generates random.
    /// </summary>
    public static long ResolveSeed(string? seedStr, out string resolvedSeedStr)
    {
        if (string.IsNullOrWhiteSpace(seedStr))
        {
            long randomSeed = GenerateRandomSeed();
            resolvedSeedStr = randomSeed.ToString(CultureInfo.InvariantCulture);
            return randomSeed;
        }

        string trimmed = seedStr.Trim();
        if (long.TryParse(trimmed, CultureInfo.InvariantCulture, out long numericSeed))
        {
            resolvedSeedStr = trimmed;
            return numericSeed;
        }

        resolvedSeedStr = trimmed;
        return (long)JavaStringHash(trimmed);
    }

    /// <summary>
    /// Loads server.properties from disk or creates a default one if it doesn't exist.
    /// </summary>
    public static ServerConfiguration Load(string filePath = "server.properties")
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var config = new ServerConfiguration();

        if (!File.Exists(filePath))
        {
            config.ResolvedSeed = GenerateRandomSeed();
            config.LevelSeed = config.ResolvedSeed.ToString(CultureInfo.InvariantCulture);
            config.HashedSeed = HashSeed(config.ResolvedSeed);
            config.Save(filePath);
            return config;
        }

        bool seedNeedsSave = false;
        var lines = File.ReadAllLines(filePath);
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith('!'))
            {
                continue;
            }

            int eqIndex = line.IndexOf('=', StringComparison.Ordinal);
            if (eqIndex < 0)
            {
                continue;
            }

            string key = line.Substring(0, eqIndex).Trim();
            string value = line.Substring(eqIndex + 1).Trim();

            switch (key.ToUpperInvariant())
            {
                case "SERVER-PORT":
                case "PORT":
                    if (int.TryParse(value, CultureInfo.InvariantCulture, out int port))
                    {
                        config.ServerPort = port;
                    }
                    break;
                case "SERVER-IP":
                    config.ServerIp = value;
                    break;
                case "LEVEL-NAME":
                    config.LevelName = value;
                    break;
                case "LEVEL-SEED":
                    config.LevelSeed = value;
                    break;
                case "GAMEMODE":
                    config.GameMode = value;
                    break;
                case "DIFFICULTY":
                    config.Difficulty = value;
                    break;
                case "MAX-PLAYERS":
                    if (int.TryParse(value, CultureInfo.InvariantCulture, out int maxP))
                    {
                        config.MaxPlayers = maxP;
                    }
                    break;
                case "VIEW-DISTANCE":
                    if (int.TryParse(value, CultureInfo.InvariantCulture, out int vd))
                    {
                        config.ViewDistance = vd;
                    }
                    break;
                case "SIMULATION-DISTANCE":
                    if (int.TryParse(value, CultureInfo.InvariantCulture, out int sd))
                    {
                        config.SimulationDistance = sd;
                    }
                    break;
                case "WORKER-COUNT":
                    if (int.TryParse(value, CultureInfo.InvariantCulture, out int wc))
                    {
                        config.WorkerCount = wc;
                    }
                    break;
                case "MOTD":
                    config.Motd = value;
                    break;
                case "PVP":
                    if (bool.TryParse(value, out bool pvp))
                    {
                        config.Pvp = pvp;
                    }
                    break;
                case "ALLOW-FLIGHT":
                    if (bool.TryParse(value, out bool af))
                    {
                        config.AllowFlight = af;
                    }
                    break;
                case "ONLINE-MODE":
                    if (bool.TryParse(value, out bool om))
                    {
                        config.OnlineMode = om;
                    }
                    break;
                case "WHITE-LIST":
                    if (bool.TryParse(value, out bool wl))
                    {
                        config.WhiteList = wl;
                    }
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(config.LevelSeed))
        {
            config.ResolvedSeed = GenerateRandomSeed();
            config.LevelSeed = config.ResolvedSeed.ToString(CultureInfo.InvariantCulture);
            seedNeedsSave = true;
        }
        else
        {
            config.ResolvedSeed = ResolveSeed(config.LevelSeed, out _);
        }

        config.HashedSeed = HashSeed(config.ResolvedSeed);

        if (seedNeedsSave)
        {
            config.Save(filePath);
        }

        return config;
    }

    /// <summary>
    /// Saves server.properties to disk.
    /// </summary>
    public void Save(string filePath = "server.properties")
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var sb = new StringBuilder();
        sb.AppendLine("#Minecraft server properties");
        sb.AppendLine("#Saved by AuroraDotNet Server");
        sb.Append("server-port=").AppendLine(ServerPort.ToString(CultureInfo.InvariantCulture));
        sb.Append("server-ip=").AppendLine(ServerIp);
        sb.Append("level-name=").AppendLine(LevelName);
        sb.Append("level-seed=").AppendLine(LevelSeed);
        sb.Append("gamemode=").AppendLine(GameMode);
        sb.Append("difficulty=").AppendLine(Difficulty);
        sb.Append("max-players=").AppendLine(MaxPlayers.ToString(CultureInfo.InvariantCulture));
        sb.Append("view-distance=").AppendLine(ViewDistance.ToString(CultureInfo.InvariantCulture));
        sb.Append("simulation-distance=").AppendLine(SimulationDistance.ToString(CultureInfo.InvariantCulture));
        sb.Append("worker-count=").AppendLine(WorkerCount.ToString(CultureInfo.InvariantCulture));
        sb.Append("motd=").AppendLine(Motd);
        sb.Append("pvp=").AppendLine(Pvp ? "true" : "false");
        sb.Append("allow-flight=").AppendLine(AllowFlight ? "true" : "false");
        sb.Append("online-mode=").AppendLine(OnlineMode ? "true" : "false");
        sb.Append("white-list=").AppendLine(WhiteList ? "true" : "false");

        File.WriteAllText(filePath, sb.ToString());
    }
}
