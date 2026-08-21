namespace Aurora.Protocol;

/// <summary>
/// Represents the current protocol state of a connection.
/// </summary>
public enum ConnectionState
{
    Handshaking = 0,
    Status = 1,
    Login = 2,
    Configuration = 3,
    Play = 4
}
