namespace Aurora.Protocol.Login;

public sealed class LoginAcknowledgedPacket : IPacket
{
    public int PacketId => 0x03;

    public void Read(ref PacketReader reader)
    {
        // Empty
    }

    public void Write(ref PacketWriter writer)
    {
        // Empty
    }
}
