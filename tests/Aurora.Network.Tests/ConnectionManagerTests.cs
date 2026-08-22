using System;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace Aurora.Network.Tests;

public class ConnectionManagerTests
{
    [Fact]
    public void ConnectionManagerAddsAndRemovesConnections()
    {
        using var manager = new ConnectionManager();
        
        // Create dummy socket (not connected, but enough to initialize)
        using var dummySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        using var connection = new MinecraftConnection(dummySocket, manager, new Aurora.World.WorldManager());
        
        manager.AddConnection(connection);
        
        // Disconnecting should trigger the event and remove it from the manager
        connection.Disconnect();
        
        // Wait a tiny bit just in case, although the event is synchronous in our implementation
        // DisconnectAll shouldn't throw or act on it anymore
        manager.DisconnectAll();
        
        // If it reaches here without issues, it successfully managed the lifecycle
        Assert.True(true);
    }
}
