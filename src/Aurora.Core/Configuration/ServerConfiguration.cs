namespace Aurora.Core.Configuration;

/// <summary>
/// Represents the fundamental server configuration loaded from disk.
/// </summary>
public class ServerConfiguration
{
    public int Port { get; set; } = 25565;
    public int MaxPlayers { get; set; } = 100;
    public int ViewDistance { get; set; } = 10;
    public int SimulationDistance { get; set; } = 10;
    public int WorkerCount { get; set; } = 4;
    public string WorldName { get; set; } = "world";
}
