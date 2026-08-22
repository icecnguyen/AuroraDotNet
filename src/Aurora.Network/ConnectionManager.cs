using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Aurora.Network;

/// <summary>
/// Manages active TCP connections to the server.
/// </summary>
public sealed class ConnectionManager : IDisposable
{
    private readonly ConcurrentDictionary<Guid, MinecraftConnection> _connections = new();

    public void AddConnection(MinecraftConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connections.TryAdd(connection.Id, connection);
        
        // When connection is closed, remove it
        connection.OnDisconnected += OnConnectionDisconnected;
    }

    private void OnConnectionDisconnected(object? sender, ConnectionEventArgs e)
    {
        var connection = e.Connection;
        _connections.TryRemove(connection.Id, out _);
        connection.OnDisconnected -= OnConnectionDisconnected;
    }

    public IEnumerable<MinecraftConnection> Players => _connections.Values;

    public void BroadcastPacket(Aurora.Protocol.IPacket packet, Guid? except = null)
    {
        foreach (var connection in _connections.Values)
        {
            if (except != null && connection.Id == except) continue;
            // Only broadcast to players in Play state (CurrentState == 4)
            if (connection.CurrentState == 4)
            {
                connection.SendPacket(packet);
            }
        }
    }

    public void KickPlayer(Guid id)
    {
        if (_connections.TryGetValue(id, out var connection))
        {
            connection.Disconnect();
        }
    }

    public void DisconnectAll()
    {
        foreach (var connection in _connections.Values)
        {
            connection.Disconnect();
        }
        _connections.Clear();
    }

    public void Dispose()
    {
        DisconnectAll();
    }
}
